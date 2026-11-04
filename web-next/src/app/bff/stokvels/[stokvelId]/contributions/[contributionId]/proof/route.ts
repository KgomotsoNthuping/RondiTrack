import { getApiBaseUrl } from '@/lib/api'
import {
  isUuid,
  proofDownloadHeaders,
  proofPath,
} from '@/lib/proofFiles'
import { getSession } from '@/lib/session'

type RouteContext = {
  params: Promise<{
    stokvelId: string
    contributionId: string
  }>
}

function failure(
  status: number,
  title: string,
): Response {
  return Response.json(
    { title, status },
    {
      status,
      headers: {
        'Cache-Control': 'private, no-store',
        'X-Content-Type-Options': 'nosniff',
      },
    },
  )
}

export async function GET(
  _request: Request,
  context: RouteContext,
): Promise<Response> {
  const { stokvelId, contributionId } =
    await context.params

  if (
    !isUuid(stokvelId) ||
    !isUuid(contributionId)
  ) {
    return failure(404, 'Proof not found.')
  }

  const session = await getSession()

  if (!session) {
    return failure(401, 'Your session has ended.')
  }

  let upstream: Response

  try {
    upstream = await fetch(
      `${getApiBaseUrl()}${proofPath(
        stokvelId,
        contributionId,
      )}`,
      {
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept:
            'application/pdf, image/jpeg, image/png',
        },
        cache: 'no-store',
      },
    )
  } catch {
    return failure(
      503,
      'The proof service is temporarily unavailable.',
    )
  }

  if (upstream.status === 401) {
    return failure(401, 'Your session has ended.')
  }

  if (
    upstream.status === 403 ||
    upstream.status === 404
  ) {
    return failure(404, 'Proof not found.')
  }

  if (!upstream.ok) {
    return failure(
      502,
      'The proof could not be downloaded.',
    )
  }

  const contentType = (
    upstream.headers.get('Content-Type') ?? ''
  )
    .split(';')[0]
    .trim()
    .toLowerCase()

  const headers = proofDownloadHeaders(
    contentType,
    contributionId,
  )

  if (!headers || !upstream.body) {
    return failure(
      502,
      'The proof service returned an invalid file.',
    )
  }

  return new Response(upstream.body, {
    status: 200,
    headers,
  })
}
