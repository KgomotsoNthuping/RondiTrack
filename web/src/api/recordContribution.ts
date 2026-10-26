import {
  useMutation,
  useQueryClient,
} from '@tanstack/react-query'
import { api, unwrap } from './client'
import { queryKeys } from './queryKeys'

export type RecordContributionVariables = {
  stokvelId: string
  cycleId: string
  userId: string
  amountInCents: number
  idempotencyKey: string
}

export function useRecordContribution() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: async ({
      stokvelId,
      cycleId,
      userId,
      amountInCents,
      idempotencyKey,
    }: RecordContributionVariables) => {
      const result = await api.POST(
        '/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions',
        {
          params: {
            path: {
              stokvelId,
              cycleId,
            },
            header: {
              'Idempotency-Key': idempotencyKey,
            },
          },
          body: {
            userId,
            amountInCents,
          },
        },
      )

      return unwrap(result)
    },
    retry: false,
    onSuccess: async (_result, variables) => {
      await Promise.all([
        client.invalidateQueries({
          queryKey: queryKeys.home,
        }),
        client.invalidateQueries({
          queryKey: queryKeys.stokvels,
        }),
        client.invalidateQueries({
          queryKey: queryKeys.cycles(variables.stokvelId),
        }),
        client.invalidateQueries({
          queryKey: queryKeys.contributions(
            variables.stokvelId,
          ),
        }),
      ])
    },
  })
}
