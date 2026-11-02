import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { fetchExtraPage } from './feedFetch'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('fetchExtraPage', () => {
  it('returns a valid additional page', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        Response.json({
          items: [{ id: 'item-1' }],
          nextPageToken: 'cursor-2',
        }),
      ),
    )

    const result = await fetchExtraPage<{
      items: { id: string }[]
      nextPageToken: string
    }>('/bff/activity?pageToken=cursor-1')

    expect(result.items).toEqual([
      { id: 'item-1' },
    ])

    expect(result.nextPageToken).toBe(
      'cursor-2',
    )
  })

  it('rejects an invalid pagination response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        Response.json({
          unexpected: true,
        }),
      ),
    )

    await expect(
      fetchExtraPage(
        '/bff/activity',
      ),
    ).rejects.toThrow(
      'invalid page',
    )
  })

  it('preserves the API failure status', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        Response.json(
          {
            title: 'Validation Failed',
          },
          {
            status: 400,
          },
        ),
      ),
    )

    await expect(
      fetchExtraPage(
        '/bff/activity?pageToken=invalid',
      ),
    ).rejects.toThrow(
      'Validation Failed (400)',
    )
  })

  it('redirects to login after a JSON 401', async () => {
    const assign = vi.fn()

    vi.stubGlobal('window', {
      location: {
        pathname: '/activity',
        search: '',
        assign,
      },
    })

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        Response.json(
          {
            title: 'Your session has ended.',
          },
          {
            status: 401,
          },
        ),
      ),
    )

    await expect(
      fetchExtraPage('/bff/activity'),
    ).rejects.toThrow(
      'Your session has ended.',
    )

    expect(assign).toHaveBeenCalledWith(
      '/login?expired=1&next=%2Factivity',
    )
  })
})