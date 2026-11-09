import {
  render,
  screen,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { server } from '@/test/msw'
import { HistoryFeed } from './HistoryFeed'
import type {
  ContributionHistoryPage,
} from '@/lib/historyTypes'

const stokvelId =
  '123e4567-e89b-42d3-a456-426614174001'

const memberId =
  '123e4567-e89b-42d3-a456-426614174002'

const nextPage: ContributionHistoryPage = {
  items: [
    {
      id: '123e4567-e89b-42d3-a456-426614174003',
      contributionCycleId:
        '123e4567-e89b-42d3-a456-426614174004',
      userId: memberId,
      memberName: 'Sipho Mokoena',
      amountInCents: 15050,
      status: 'Paid',
      paidAtUtc: '2026-10-15T10:00:00Z',
      cycleNumber: 1,
    },
  ],
  nextPageToken: '',
}

function renderFeed() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <HistoryFeed
        stokvelId={stokvelId}
        viewerUserId={memberId}
        viewerRole="Member"
        filters={{
          member: memberId,
          from: '2026-10-01',
          to: '2026-10-31',
        }}
        after="page-one-cursor"
        firstItemId="first-contribution"
        firstCount={1}
      >
        <article>First contribution</article>
      </HistoryFeed>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.stubGlobal(
    'IntersectionObserver',
    class {
      observe() {}
      unobserve() {}
      disconnect() {}
    },
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('HistoryFeed pagination', () => {
  it('sends the first cursor and all selected filters', async () => {
    const user = userEvent.setup()

    const requests: URL[] = []

    server.use(
      http.get(
        'http://localhost:3000/bff/stokvels/:stokvelId/contributions',
        ({ request }) => {
          requests.push(new URL(request.url))

          return HttpResponse.json(nextPage)
        },
      ),
    )

    renderFeed()

    expect(
      screen.getByText('First contribution'),
    ).toBeInTheDocument()

    expect(requests).toHaveLength(0)

    await user.click(
      screen.getByRole('button', {
        name: 'Load more',
      }),
    )

    expect(
      await screen.findByText('Sipho Mokoena'),
    ).toBeInTheDocument()

    expect(requests).toHaveLength(1)

    const query = requests[0].searchParams

    expect(query.get('pageToken')).toBe(
      'page-one-cursor',
    )

    expect(query.get('member')).toBe(memberId)
    expect(query.get('from')).toBe('2026-10-01')
    expect(query.get('to')).toBe('2026-10-31')

    expect(
      screen.getByRole('status'),
    ).toHaveTextContent('Showing 2 contributions.')

    expect(
      screen.getByRole('status'),
    ).toHaveTextContent(
      'All available contributions are shown.',
    )
  })

  it('offers a retry after an error without claiming that all records loaded', async () => {
    const user = userEvent.setup()

    let requests = 0

    server.use(
      http.get(
        'http://localhost:3000/bff/stokvels/:stokvelId/contributions',
        () => {
          requests += 1

          if (requests === 1) {
            return HttpResponse.json(
              {
                title: 'History temporarily unavailable',
              },
              { status: 503 },
            )
          }

          return HttpResponse.json(nextPage)
        },
      ),
    )

    renderFeed()

    await user.click(
      screen.getByRole('button', {
        name: 'Load more',
      }),
    )

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent(
      'History temporarily unavailable',
    )

    expect(
      screen.getByRole('status'),
    ).toHaveTextContent(
      'More records are available.',
    )

    expect(
      screen.queryByText(
        /All available contributions are shown/,
      ),
    ).not.toBeInTheDocument()

    await user.click(
      screen.getByRole('button', {
        name: 'Retry loading',
      }),
    )

    expect(
      await screen.findByText('Sipho Mokoena'),
    ).toBeInTheDocument()

    expect(requests).toBe(2)

    expect(
      screen.getByRole('status'),
    ).toHaveTextContent('Showing 2 contributions.')
  })
})
