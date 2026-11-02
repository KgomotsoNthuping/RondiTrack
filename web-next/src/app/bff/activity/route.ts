import type { NextRequest } from 'next/server'

import { forwardAuthenticatedGet } from '@/lib/bff'
import { ACTIVITY_PAGE_SIZE } from '@/lib/activityTypes'

export async function GET(
  request: NextRequest,
): Promise<Response> {
  const pageToken =
    request.nextUrl.searchParams.get('pageToken')

  const query = new URLSearchParams({
    pageSize: String(ACTIVITY_PAGE_SIZE),
  })

  if (pageToken) {
    query.set('pageToken', pageToken)
  }

  return forwardAuthenticatedGet(
    '/api/v1/me/activity',
    query,
  )
}
