import { useQuery } from '@tanstack/react-query'
import { api, unwrap } from './client'
import { queryKeys } from './queryKeys'

export type ContributionMember = {
  id: string
  fullName: string
}

export function useContributionMembers(
  stokvelId: string | null,
  enabled: boolean,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.stokvel(stokvelId ?? ''),
      'members',
    ],
    enabled: Boolean(stokvelId) && enabled,
    queryFn: async (): Promise<ContributionMember[]> => {
      if (!stokvelId) {
        throw new Error('A stokvel ID is required.')
      }

      const members: ContributionMember[] = []
      const seenTokens = new Set<string>()
      let pageToken = ''

      do {
        const result = await api.GET(
          '/api/v1/stokvels/{stokvelId}/members',
          {
            params: {
              path: { stokvelId },
              query: {
                PageSize: 100,
                PageToken: pageToken,
              },
            },
          },
        )

        const page = await unwrap(result)

        members.push(
          ...page.items.map(member => ({
            id: member.id,
            fullName: member.fullName,
          })),
        )

        const nextToken = page.nextPageToken

        if (!nextToken) {
          break
        }

        if (seenTokens.has(nextToken)) {
          throw new Error(
            'The member list returned a repeated page token.',
          )
        }

        seenTokens.add(nextToken)
        pageToken = nextToken
      } while (true)

      return members
    },
  })
}
