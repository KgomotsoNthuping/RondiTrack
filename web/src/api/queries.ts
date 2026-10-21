
import {
  useInfiniteQuery,
  useQuery,
} from '@tanstack/react-query'

import { api, unwrap } from './client'
import { queryKeys } from './queryKeys'

const PAGE_SIZE = 20

// Signed-in member's profile.
export function useMe() {
  return useQuery({
    queryKey: queryKeys.me,

    queryFn: async () => {
      const result = await api.GET('/api/v1/me')
      return unwrap(result)
    },
  })
}

// Signed-in member's stokvel summary and balances.
export function useHome() {
  return useQuery({
    queryKey: queryKeys.home,

    queryFn: async () => {
      const result = await api.GET('/api/v1/me/home')
      return unwrap(result)
    },
  })
}

// This uses /me/home because it returns only stokvels
// belonging to the authenticated member.
export function useStokvels() {
  return useQuery({
    queryKey: queryKeys.stokvels,

    queryFn: async () => {
      const result = await api.GET('/api/v1/me/home')
      const data = await unwrap(result)

      return data.stokvels
    },
  })
}

// Retrieves the selected stokvel's details.
export function useStokvel(stokvelId: string | null) {
  return useQuery({
    queryKey: queryKeys.stokvel(stokvelId ?? ''),

    enabled: Boolean(stokvelId),

    queryFn: async () => {
      if (!stokvelId) {
        throw new Error('A stokvel ID is required')
      }

      const result = await api.GET(
        '/api/v1/stokvels/{id}',
        {
          params: {
            path: {
              id: stokvelId,
            },
          },
        },
      )

      return unwrap(result)
    },
  })
}

// Retrieves all contribution cycles for a stokvel.
export function useCycles(stokvelId: string | null) {
  return useQuery({
    queryKey: queryKeys.cycles(stokvelId ?? ''),

    enabled: Boolean(stokvelId),

    queryFn: async () => {
      if (!stokvelId) {
        throw new Error('A stokvel ID is required')
      }

      const result = await api.GET(
        '/api/v1/stokvels/{stokvelId}/cycles',
        {
          params: {
            path: {
              stokvelId,
            },
          },
        },
      )

      return unwrap(result)
    },
  })
}

// Contributions are paginated.
// Each stokvel gets its own infinite-query cache.
export function useContributions(
  stokvelId: string | null,
) {
  return useInfiniteQuery({
    queryKey:
      queryKeys.contributions(stokvelId ?? ''),

    enabled: Boolean(stokvelId),

    initialPageParam: '',

    queryFn: async ({ pageParam }) => {
      if (!stokvelId) {
        throw new Error('A stokvel ID is required')
      }

      const result = await api.GET(
        '/api/v1/stokvels/{stokvelId}/contributions',
        {
          params: {
            path: {
              stokvelId,
            },
            query: {
              pageSize: PAGE_SIZE,
              pageToken: pageParam,
            },
          },
        },
      )

      return unwrap(result)
    },

    getNextPageParam: (lastPage) => {
      // Your backend returns "" at the final page.
      const nextToken = lastPage.nextPageToken

      return nextToken === ''
        ? undefined
        : nextToken
    },
  })
}
