import 'server-only'

import { redirect } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
import { getApiBaseUrl, signedInApi } from '@/lib/api'
import {
  ACTIVITY_PAGE_SIZE,
  type MemberActivityPage,
} from '@/lib/activityTypes'

export async function loadMemberActivityPage():
  Promise<MemberActivityPage | ApiError> {
  const { session } = await signedInApi()

  const query = new URLSearchParams({
    pageSize: String(ACTIVITY_PAGE_SIZE),
  })

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/me/activity?${query.toString()}`,
      {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )
  } catch {
    return new ApiError(0)
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (!response.ok) {
    return ApiError.fromResponse(response)
  }

  const result: unknown = await response.json()

  if (
    result === null ||
    typeof result !== 'object' ||
    !('items' in result) ||
    !Array.isArray(result.items) ||
    !('nextPageToken' in result) ||
    typeof result.nextPageToken !== 'string'
  ) {
    throw new Error(
      'Invalid member activity response.',
    )
  }

  return result as MemberActivityPage
}
