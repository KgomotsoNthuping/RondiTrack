
import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './ApiError'

function shouldRetry(
  failureCount: number,
  error: unknown,
): boolean {
  // Never retry client errors:
  // 400, 401, 403, 404, 409, 429, etc.
  if (
    error instanceof ApiError &&
    error.status >= 400 &&
    error.status < 500
  ) {
    return false
  }

  // Retry temporary network/server failures twice.
  return failureCount < 2
}

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: shouldRetry,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },

    mutations: {
      retry: false,
    },
  },
})
