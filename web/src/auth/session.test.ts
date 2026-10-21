
import {
  beforeEach,
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import type * as Session from './session'

const BASE_URL = 'http://localhost:5227'

type SessionModule = typeof Session

type FakeApiOptions = {
  failRefresh?: boolean
  acceptTokens?: string[]
}

function createFakeApi(options: FakeApiOptions = {}) {
  let refreshCount = 0
  let protectedCount = 0
  let currentRefreshToken = 'new'

  const acceptedTokens = options.acceptTokens ?? ['new']

  const fetchMock = vi.fn(
    async (input: RequestInfo | URL, init?: RequestInit) => {
      // openapi-fetch passes Request objects, not just URL strings.
      const request = new Request(input, init)
      const url = new URL(request.url)

      if (url.pathname.endsWith('/auth/login')) {
        return new Response(
          JSON.stringify({
            accessToken: 'old',
            accessTokenExpiresAtUtc:
              '2026-10-21T16:00:00Z',
          }),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        )
      }

      if (url.pathname.endsWith('/auth/refresh')) {
        refreshCount++

        // Make concurrent requests overlap.
        await new Promise((resolve) =>
          setTimeout(resolve, 20),
        )

        if (options.failRefresh) {
          return new Response(null, { status: 401 })
        }

        const issuedToken = currentRefreshToken

        return new Response(
          JSON.stringify({
            accessToken: issuedToken,
            accessTokenExpiresAtUtc:
              '2026-10-21T17:00:00Z',
          }),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        )
      }

      if (url.pathname.endsWith('/auth/logout')) {
        return new Response(null, { status: 204 })
      }

      protectedCount++

      const authorization =
        request.headers.get('Authorization')

      const body =
        request.method === 'POST'
          ? await request.text()
          : null

      if (
        authorization !== null &&
        acceptedTokens.some(
          (token) =>
            authorization === `Bearer ${token}`,
        )
      ) {
        return new Response(
          JSON.stringify({ ok: true, body }),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        )
      }

      return new Response(null, { status: 401 })
    },
  )

  return {
    fetchMock,
    getRefreshCount: () => refreshCount,
    getProtectedCount: () => protectedCount,
    setRefreshToken: (token: string) => {
      currentRefreshToken = token
      acceptedTokens.push(token)
    },
  }
}

async function loadSession(): Promise<SessionModule> {
  // A fresh module gives each test independent in-memory tokens.
  vi.resetModules()
  return import('./session')
}

describe('RondiTrack browser session', () => {
  beforeEach(() => {
    vi.resetModules()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('five parallel 401s cause exactly one refresh', async () => {
    const api = createFakeApi()

    vi.stubGlobal('fetch', api.fetchMock)

    const session = await loadSession()

    await session.login('member@example.com', 'password')

    const responses = await Promise.all(
      Array.from({ length: 5 }, (_, index) =>
        session.authFetch(
          `${BASE_URL}/api/v1/protected/${index}`,
        ),
      ),
    )

    expect(api.getRefreshCount()).toBe(1)
    expect(responses.map((response) => response.status)).toEqual(
      [200, 200, 200, 200, 200],
    )
  })

  it('a later expiry starts a new refresh', async () => {
    const api = createFakeApi()

    vi.stubGlobal('fetch', api.fetchMock)

    const session = await loadSession()

    await session.login('member@example.com', 'password')

    const firstBatch = await Promise.all(
      Array.from({ length: 5 }, (_, index) =>
        session.authFetch(
          `${BASE_URL}/api/v1/protected/${index}`,
        ),
      ),
    )

    expect(firstBatch.every((r) => r.status === 200)).toBe(true)
    expect(api.getRefreshCount()).toBe(1)

    // Simulate the next access token expiring.
    // The fake API now accepts only the newer token.
    const nextApi = createFakeApi({
      acceptTokens: ['newer'],
    })

    nextApi.setRefreshToken('newer')

    // Preserve refresh counts across both batches.
    let additionalRefreshes = 0

    const secondFetch = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const request = new Request(input, init)

        if (
          new URL(request.url).pathname.endsWith(
            '/auth/refresh',
          )
        ) {
          additionalRefreshes++
        }

        return nextApi.fetchMock(input, init)
      },
    )

    vi.stubGlobal('fetch', secondFetch)

    const secondBatch = await Promise.all([
      session.authFetch(`${BASE_URL}/api/v1/protected/a`),
      session.authFetch(`${BASE_URL}/api/v1/protected/b`),
    ])

    expect(secondBatch.map((response) => response.status)).toEqual([
      200,
      200,
    ])

    expect(api.getRefreshCount() + additionalRefreshes).toBe(2)
  })

  it('a failed refresh signs out once', async () => {
    const api = createFakeApi({
      failRefresh: true,
    })

    vi.stubGlobal('fetch', api.fetchMock)

    const session = await loadSession()

    await session.login('member@example.com', 'password')

    const signedOut = vi.fn()
    const unsubscribe = session.onSignedOut(signedOut)

    const response = await session.authFetch(
      `${BASE_URL}/api/v1/protected`,
    )

    expect(response.status).toBe(401)
    expect(api.getRefreshCount()).toBe(1)
    expect(api.getProtectedCount()).toBe(1)
    expect(signedOut).toHaveBeenCalledTimes(1)

    unsubscribe()
  })

  it('no token means no infinite refresh loop', async () => {
    const api = createFakeApi({
      failRefresh: true,
    })

    vi.stubGlobal('fetch', api.fetchMock)

    const session = await loadSession()

    // No login: simulate a new visitor without a cookie.
    const response = await session.authFetch(
      `${BASE_URL}/api/v1/protected`,
    )

    expect(response.status).toBe(401)
    expect(api.getRefreshCount()).toBe(1)
    expect(api.getProtectedCount()).toBe(1)
  })

  it('a POST body survives the authentication retry', async () => {
    const api = createFakeApi()

    vi.stubGlobal('fetch', api.fetchMock)

    const session = await loadSession()

    await session.login('member@example.com', 'password')

    const payload = {
      stokvelId: 'stokvel-001',
      amountInCents: 15050,
    }

    const response = await session.authFetch(
      `${BASE_URL}/api/v1/contributions`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      },
    )

    expect(response.status).toBe(200)
    expect(api.getRefreshCount()).toBe(1)

    const result = (await response.json()) as {
      ok: boolean
      body: string
    }

    expect(JSON.parse(result.body)).toEqual(payload)
  })
})
