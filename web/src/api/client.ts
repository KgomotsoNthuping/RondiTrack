
import createClient from 'openapi-fetch'
import type { paths } from './schema'
import { authFetch } from '../auth/session'
import { ApiError, type ApiProblem } from './ApiError'

const baseUrl = import.meta.env.VITE_API_BASE_URL

if (!baseUrl) {
  throw new Error('VITE_API_BASE_URL is not configured')
}

export const api = createClient<paths>({
  baseUrl: baseUrl.replace(/\/$/, ''),
  fetch: authFetch,
  credentials: 'include',
})

type ApiResult<T> = {
  data?: T
  error?: unknown
  response: Response
}

export async function unwrap<T>(
  result: ApiResult<T>,
): Promise<T> {
  if (!result.response.ok || result.error != null) {
    const contentType =
      result.response.headers.get('Content-Type') ?? ''

    let problem: ApiProblem | null = null

    if (
      result.error !== null &&
      typeof result.error === 'object'
    ) {
      problem = result.error as ApiProblem
    } else if (contentType.includes('json')) {
      try {
        problem =
          (await result.response.clone().json()) as ApiProblem
      } catch {
        // Invalid or unreadable problem response.
      }
    }

    throw new ApiError(
      result.response.status,
      problem,
      result.response.headers.get('Retry-After'),
    )
  }

  if (result.data === undefined) {
    throw new ApiError(result.response.status, {
      title: 'No data received',
    })
  }

  return result.data
}
