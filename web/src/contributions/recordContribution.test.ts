
import {
  afterAll,
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { login } from '../auth/session'
import { sendContribution } from '../api/recordContribution'
import { parseRetryAfter } from '../lib/errorMessages'
import { toContributionRequest } from '../forms/contribution'
import { PaymentIntentTracker } from './paymentIntent'

const API = (
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5227'
).replace(/\/$/, '')

const STOKVEL_ID = '123e4567-e89b-42d3-a456-426614174001'
const CYCLE_ID = '123e4567-e89b-42d3-a456-426614174002'
const MEMBER_ID = '123e4567-e89b-42d3-a456-426614174003'

const POST_URL =
  `${API}/api/v1/stokvels/:stokvelId/cycles/:cycleId/contributions`

const recorded = {
  id: '123e4567-e89b-42d3-a456-426614174004',
  stokvelId: STOKVEL_ID,
  userId: MEMBER_ID,
  contributionCycleId: CYCLE_ID,
  amountInCents: 15050,
  status: 'Paid',
  paidAtUtc: '2026-10-26T00:00:00Z',
}

const server = setupServer(
  http.post(`${API}/api/v1/auth/login`, () => {
    return HttpResponse.json({
      accessToken: 'old-token',
      accessTokenExpiresAtUtc: '2027-01-01T00:00:00Z',
    })
  }),
  http.post(`${API}/api/v1/auth/refresh`, () => {
    return HttpResponse.json({
      accessToken: 'new-token',
      accessTokenExpiresAtUtc: '2027-01-01T00:00:00Z',
    })
  }),
)

function payment(
  tracker: PaymentIntentTracker,
  amountInCents = 15050,
  cycleId = CYCLE_ID,
) {
  const details = {
    cycleId,
    userId: MEMBER_ID,
    amountInCents,
  }

  return {
    stokvelId: STOKVEL_ID,
    ...details,
    idempotencyKey: tracker.keyFor(details),
  }
}

beforeAll(() => {
  server.listen({
    onUnhandledRequest: 'error',
  })
})

beforeEach(async () => {
  await login('member@ronditrack.test', 'test-password')
})

afterEach(() => {
  server.resetHandlers()
})

afterAll(() => {
  server.close()
})

describe('contribution recording', () => {
  it('reuses the same key after a network failure', async () => {
    const tracker = new PaymentIntentTracker()
    const keysSeen: (string | null)[] = []
    let attempts = 0

    server.use(
      http.post(POST_URL, ({ request }) => {
        keysSeen.push(
          request.headers.get('Idempotency-Key'),
        )

        attempts++

        return attempts === 1
          ? HttpResponse.error()
          : HttpResponse.json(recorded, {
              status: 201,
            })
      }),
    )

    await expect(
      sendContribution(payment(tracker)),
    ).rejects.toThrow()

    await expect(
      sendContribution(payment(tracker)),
    ).resolves.toMatchObject(recorded)

    expect(keysSeen).toHaveLength(2)
    expect(keysSeen[0]).toBeTruthy()
    expect(keysSeen[0]).toBe(keysSeen[1])
  })

  it('uses a new key for the next contribution', async () => {
    const tracker = new PaymentIntentTracker()
    const keysSeen: string[] = []

    server.use(
      http.post(POST_URL, ({ request }) => {
        keysSeen.push(
          request.headers.get('Idempotency-Key') ?? '',
        )

        return HttpResponse.json(recorded, {
          status: 201,
        })
      }),
    )

    await sendContribution(payment(tracker))

    tracker.clear()

    await sendContribution(
      payment(
        tracker,
        15050,
        '123e4567-e89b-42d3-a456-426614174005',
      ),
    )

    expect(keysSeen).toHaveLength(2)
    expect(keysSeen[0]).toBeTruthy()
    expect(keysSeen[1]).toBeTruthy()
    expect(keysSeen[0]).not.toBe(keysSeen[1])
  })

  it('keeps the key after rate limiting', async () => {
    const tracker = new PaymentIntentTracker()
    const keysSeen: (string | null)[] = []
    let attempts = 0

    server.use(
      http.post(POST_URL, ({ request }) => {
        keysSeen.push(
          request.headers.get('Idempotency-Key'),
        )

        attempts++

        if (attempts === 1) {
          return HttpResponse.json(
            {
              title: 'Too many requests',
              status: 429,
            },
            {
              status: 429,
              headers: {
                'Retry-After': '1',
              },
            },
          )
        }

        return HttpResponse.json(recorded, {
          status: 201,
        })
      }),
    )

    await expect(
      sendContribution(payment(tracker)),
    ).rejects.toMatchObject({
      status: 429,
      retryAfter: '1',
    })

    expect(parseRetryAfter('1')).toBe(1)

    await sendContribution(payment(tracker))

    expect(keysSeen).toHaveLength(2)
    expect(keysSeen[0]).toBeTruthy()
    expect(keysSeen[0]).toBe(keysSeen[1])
  })

  it('creates a new key when the amount changes', async () => {
    const tracker = new PaymentIntentTracker()
    const keysSeen: (string | null)[] = []
    const amountsSeen: number[] = []
    let attempts = 0

    server.use(
      http.post(POST_URL, async ({ request }) => {
        const body = (await request.json()) as {
          amountInCents: number
        }

        amountsSeen.push(body.amountInCents)
        keysSeen.push(
          request.headers.get('Idempotency-Key'),
        )

        attempts++

        return attempts === 1
          ? HttpResponse.error()
          : HttpResponse.json(
              {
                ...recorded,
                amountInCents: body.amountInCents,
              },
              {
                status: 201,
              },
            )
      }),
    )

    await expect(
      sendContribution(payment(tracker, 15050)),
    ).rejects.toThrow()

    await sendContribution(payment(tracker, 16050))

    expect(amountsSeen).toEqual([15050, 16050])
    expect(keysSeen[0]).toBeTruthy()
    expect(keysSeen[1]).toBeTruthy()
    expect(keysSeen[0]).not.toBe(keysSeen[1])
  })

  it('preserves the key during a 401 refresh retry', async () => {
    const tracker = new PaymentIntentTracker()
    const keysSeen: (string | null)[] = []
    const authorizations: (string | null)[] = []
    let refreshes = 0

    server.use(
      http.post(`${API}/api/v1/auth/refresh`, () => {
        refreshes++

        return HttpResponse.json({
          accessToken: 'new-token',
          accessTokenExpiresAtUtc:
            '2027-01-01T00:00:00Z',
        })
      }),
      http.post(POST_URL, ({ request }) => {
        keysSeen.push(
          request.headers.get('Idempotency-Key'),
        )

        const authorization =
          request.headers.get('Authorization')

        authorizations.push(authorization)

        if (authorization === 'Bearer old-token') {
          return HttpResponse.json(
            {
              title: 'Unauthorized',
            },
            {
              status: 401,
            },
          )
        }

        return HttpResponse.json(recorded, {
          status: 201,
        })
      }),
    )

    await sendContribution(payment(tracker))

    expect(refreshes).toBe(1)
    expect(keysSeen).toHaveLength(2)
    expect(keysSeen[0]).toBeTruthy()
    expect(keysSeen[0]).toBe(keysSeen[1])

    expect(authorizations).toEqual([
      'Bearer old-token',
      'Bearer new-token',
    ])
  })

  it('sends exact integer cents in the request body', async () => {
    const tracker = new PaymentIntentTracker()
    const requestBodies: unknown[] = []

    server.use(
      http.post(POST_URL, async ({ request }) => {
        requestBodies.push(await request.json())

        return HttpResponse.json(recorded, {
          status: 201,
        })
      }),
    )

    const values = toContributionRequest({
      userId: MEMBER_ID,
      amount: '150.50',
    })

    await sendContribution({
      stokvelId: STOKVEL_ID,
      cycleId: CYCLE_ID,
      ...values,
      idempotencyKey: tracker.keyFor({
        cycleId: CYCLE_ID,
        userId: values.userId,
        amountInCents: values.amountInCents,
      }),
    })

    expect(requestBodies).toHaveLength(1)

    expect(requestBodies[0]).toMatchObject({
      userId: MEMBER_ID,
      amountInCents: 15050,
    })

    const body = requestBodies[0] as {
      amountInCents: unknown
    }

    expect(typeof body.amountInCents).toBe('number')
  })
})
