import { ApiError } from '../api/ApiError'

export type ErrorDescription = {
  title: string
  message: string
  reference: string | null
  retryAfterSeconds: number | null
  canRetry: boolean
  fieldErrors: Record<string, string[]>
}

export function parseRetryAfter(
  header: string | null,
  now: number = Date.now(),
): number | null {
  if (header === null) {
    return null
  }

  const value = header.trim()

  if (value.length === 0) {
    return null
  }

  if (/^\d+$/.test(value)) {
    const seconds = Number(value)

    return Number.isSafeInteger(seconds)
      ? seconds
      : null
  }

  const httpDatePattern =
    /^(Mon|Tue|Wed|Thu|Fri|Sat|Sun), \d{2} (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) \d{4} \d{2}:\d{2}:\d{2} GMT$/

  if (!httpDatePattern.test(value)) {
    return null
  }

  const date = Date.parse(value)

  if (Number.isNaN(date)) {
    return null
  }

  if (new Date(date).toUTCString() !== value) {
    return null
  }

  return Math.max(
    0,
    Math.ceil((date - now) / 1000),
  )
}

export function describeError(error: unknown): ErrorDescription {
  if (!(error instanceof ApiError)) {
    return {
      title: "Can't reach RondiTrack",
      message: 'Check your connection and try again.',
      reference: null,
      retryAfterSeconds: null,
      canRetry: true,
      fieldErrors: {},
    }
  }

  const status = error.status
  const reference = error.problem?.correlationId ?? null
  const retryAfterSeconds = parseRetryAfter(error.retryAfter)
  const fieldErrors = error.problem?.errors ?? {}

  if (status === 0) {
    return {
      title: "Can't reach RondiTrack",
      message: 'Check your connection and try again.',
      reference: null,
      retryAfterSeconds: null,
      canRetry: true,
      fieldErrors: {},
    }
  }

  if (status === 400) {
    return {
      title: 'Please check your information',
      message: 'Correct the highlighted information and try again.',
      reference,
      retryAfterSeconds: null,
      canRetry: false,
      fieldErrors,
    }
  }

  if (status === 401) {
    return {
      title: 'Session ended',
      message: 'Please sign in again.',
      reference,
      retryAfterSeconds: null,
      canRetry: false,
      fieldErrors: {},
    }
  }

  if (status === 403) {
    return {
      title: 'Access denied',
      message: 'You do not have permission to perform this action.',
      reference,
      retryAfterSeconds: null,
      canRetry: false,
      fieldErrors: {},
    }
  }

  if (status === 404) {
    return {
      title: 'Stokvel not found',
      message:
        'This stokvel may no longer exist, or you may no longer be a member.',
      reference,
      retryAfterSeconds: null,
      canRetry: false,
      fieldErrors: {},
    }
  }

  if (status === 429) {
    return {
      title: 'Too many requests',
      message:
        retryAfterSeconds !== null
          ? `Too many requests. Try again in ${retryAfterSeconds} seconds.`
          : 'Too many requests. Please try again later.',
      reference,
      retryAfterSeconds,
      canRetry: false,
      fieldErrors: {},
    }
  }

  return {
    title: 'Something went wrong',
    message:
      'RondiTrack could not complete your request. Please try again or contact support.',
    reference,
    retryAfterSeconds: null,
    canRetry: status >= 500,
    fieldErrors: {},
  }
}
