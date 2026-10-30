import 'server-only'

import { notFound, redirect } from 'next/navigation'
import { ApiError } from '../api/ApiError'
import { getApiBaseUrl, signedInApi } from './api'
import type { SouthAfricanUtcRange } from './southAfricanDates'
import type { ContributionHistoryPage } from './historyTypes'
import { HISTORY_PAGE_SIZE } from './historyFilters'

export async function loadContributionHistoryPage(
  stokvelId: string,
  member: string | null,
  range: SouthAfricanUtcRange,
  pageToken?: string,
): Promise<ContributionHistoryPage | ApiError> {
  const { session } = await signedInApi()

  const query = new URLSearchParams({
    pageSize: String(HISTORY_PAGE_SIZE),
  })

  if (member !== null) {
    query.set('member', member)
  }

  if (range.fromUtc !== null) {
    query.set('from', range.fromUtc)
  }

  if (range.toUtc !== null) {
    query.set('to', range.toUtc)
  }

  if (pageToken) {
    query.set('pageToken', pageToken)
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/stokvels/${encodeURIComponent(stokvelId)}/contributions?${query.toString()}`,
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

  if (
    response.status === 403 ||
    response.status === 404
  ) {
    notFound()
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
      'Invalid contribution history response.',
    )
  }

  return result as ContributionHistoryPage
}
