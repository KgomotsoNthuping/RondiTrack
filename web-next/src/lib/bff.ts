import 'server-only'

import { getSession } from '@/lib/session'
import { getApiBaseUrl } from '@/lib/api'

function jsonFailure(
  status: number,
  title: string,
): Response {
  return Response.json(
    { title, status },
    {
      status,
      headers: {
        'Cache-Control': 'no-store',
      },
    },
  )
}

export async function forwardAuthenticatedGet(
  path: string,
  query: URLSearchParams,
): Promise<Response> {
  const session = await getSession()

  if (!session) {
    return jsonFailure(
      401,
      'Your session has ended.',
    )
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}${path}?${query.toString()}`,
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
    return jsonFailure(
      503,
      'The API is temporarily unavailable.',
    )
  }

  const contentType =
    response.headers.get('Content-Type') ?? ''

  if (!contentType.includes('json')) {
    return jsonFailure(
      response.ok ? 502 : response.status,
      'The API returned an unexpected response.',
    )
  }

  let body: unknown

  try {
    body = await response.json()
  } catch {
    return jsonFailure(
      502,
      'The API returned invalid JSON.',
    )
  }

  const headers = new Headers({
    'Content-Type': 'application/json',
    'Cache-Control': 'private, no-store',
  })

  const retryAfter =
    response.headers.get('Retry-After')

  if (retryAfter) {
    headers.set('Retry-After', retryAfter)
  }

  return new Response(
    JSON.stringify(body),
    {
      status: response.status,
      headers,
    },
  )
}
