import 'server-only'
import createClient from 'openapi-fetch'
import { notFound, redirect } from 'next/navigation'
import type { paths } from '@/api/schema'
import {
  ApiError,
  type ApiProblem,
} from '@/api/ApiError'
import { getSession } from './session'

export function getApiBaseUrl(): string {
  const value = process.env.API_BASE_URL?.trim()

  if (!value) {
    throw new Error('API_BASE_URL is not configured.')
  }

  return value.replace(/\/$/, '')
}

export async function signedInApi() {
  const session = await getSession()

  if (!session) {
    redirect('/login')
  }

  const api = createClient<paths>({
    baseUrl: getApiBaseUrl(),
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
    fetch: (request: Request): Promise<Response> =>
      fetch(request, {
        cache: 'no-store',
      }),
  })

  return {
    api,
    session,
  }
}

function asProblem(
  value: unknown,
): ApiProblem | null {
  if (
    value === null ||
    typeof value !== 'object' ||
    Array.isArray(value)
  ) {
    return null
  }

  const candidate = value as Record<string, unknown>

  return {
    title:
      typeof candidate.title === 'string'
        ? candidate.title
        : undefined,
    detail:
      typeof candidate.detail === 'string'
        ? candidate.detail
        : undefined,
    correlationId:
      typeof candidate.correlationId === 'string'
        ? candidate.correlationId
        : undefined,
    errors:
      candidate.errors &&
      typeof candidate.errors === 'object' &&
      !Array.isArray(candidate.errors)
        ? candidate.errors as Record<string, string[]>
        : undefined,
  }
}


export function readOrFail<T>(
  result: {
    data?: T
    error?: unknown
    response: Response
  },
): T {
  const { response, data, error } = result

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (response.status === 404) {
    notFound()
  }

  if (response.ok && data !== undefined) {
    return data
  }

  if (!response.ok) {
    throw new ApiError(
      response.status,
      asProblem(error),
      response.headers.get('Retry-After'),
    )
  }

  throw new Error('The API returned an empty response.')
}
