import 'server-only'
import { shouldHideStokvel } from '@/lib/stokvelVisibility'
import { notFound } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
import {
  toRecordableMembers,
  type RecordableMember,
} from '@/lib/contributionMembers'
import type {
  ContributionItem,
  MeHomeResponse,
  StokvelDetail,
} from '@/api/types'
import { readOrFail, signedInApi } from '@/lib/api'

export async function getMemberHome(): Promise<MeHomeResponse> {
  const { api } = await signedInApi()

  const result = await api.GET('/api/v1/me/home')

  return readOrFail(result)
}

export async function getStokvelDetail(
  id: string,
): Promise<StokvelDetail> {
  const { api } = await signedInApi()

  const result = await api.GET('/api/v1/stokvels/{id}', {
    params: {
      path: { id },
    },
  })

  if (shouldHideStokvel(result.response.status)) 
    {notFound()}

  return readOrFail(result)
}

export async function loadMemberHome() {
  try {
    return await getMemberHome()
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}

export async function loadStokvelDetail(id: string) {
  try {
    return await getStokvelDetail(id)
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}

export async function loadRecentContributions(
  stokvelId: string,
): Promise<ContributionItem[] | ApiError> {
  try {
    const { api } = await signedInApi()

    const result = await api.GET(
      '/api/v1/stokvels/{stokvelId}/contributions',
      {
        params: {
          path: {
            stokvelId,
          },
          query: {
            pageSize: 5,
          },
        },
      },
    )

    const page = readOrFail(result)

    return page.items
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}


export async function loadRecordableMembers(
  stokvelId: string,
): Promise<RecordableMember[] | ApiError> {
  try {
    const { api } = await signedInApi()

    const members: RecordableMember[] = []
    const seenTokens = new Set<string>()

    let pageToken: string | undefined

    while (true) {
      const result = await api.GET(
        '/api/v1/stokvels/{stokvelId}/members',
        {
          params: {
            path: {
              stokvelId,
            },
            query: {
              PageSize: 100,
              Sort: 'fullName',
              PageToken: pageToken,
            },
          },
        },
      )

      const page = readOrFail(result)

      members.push(
        ...toRecordableMembers(page.items),
      )

      const nextToken = page.nextPageToken

      if (!nextToken) {
        return members
      }

      if (
        seenTokens.has(nextToken) ||
        seenTokens.size >= 100
      ) {
        throw new Error(
          'Invalid member pagination response.',
        )
      }

      seenTokens.add(nextToken)
      pageToken = nextToken
    }
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}
