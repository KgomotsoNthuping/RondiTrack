import type { NextRequest } from 'next/server'

import { forwardAuthenticatedGet } from '@/lib/bff'
import {
  HISTORY_PAGE_SIZE,
  loadHistoryFilters,
} from '@/lib/historyFilters'
import { southAfricanDaysToUtcRange } from '@/lib/southAfricanDates'

type RouteContext = {
  params: Promise<{ stokvelId: string }>
}

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export async function GET(
  request: NextRequest,
  context: RouteContext,
): Promise<Response> {
  const { stokvelId } = await context.params

  if (!uuidPattern.test(stokvelId)) {
    return Response.json(
      { title: 'Invalid stokvel ID.' },
      { status: 400 },
    )
  }

  const filters = await loadHistoryFilters(
    request.nextUrl.searchParams,
  )

  const range = southAfricanDaysToUtcRange(
    filters.from,
    filters.to,
  )

  const pageToken =
    request.nextUrl.searchParams.get('pageToken')

  const query = new URLSearchParams({
    pageSize: String(HISTORY_PAGE_SIZE),
  })

  if (filters.member) {
    query.set('member', filters.member)
  }

  if (range.fromUtc) {
    query.set('from', range.fromUtc)
  }

  if (range.toUtc) {
    query.set('to', range.toUtc)
  }

  if (pageToken) {
    query.set('pageToken', pageToken)
  }

  return forwardAuthenticatedGet(
    `/api/v1/stokvels/${stokvelId}/contributions`,
    query,
  )
}
