import { ApiError } from '@/api/ApiError'
import { parseRetryAfter } from '@/lib/errorMessages'

export type ContributionFailure = {
  status: 'error' | 'uncertain'
  message: string
  reference: string | null
  fieldErrors: Record<string, string[]>
}

function contributionFieldErrors(
  errors: Record<string, string[]> | undefined,
): Record<string, string[]> {
  const result: Record<string, string[]> = {}

  for (const [key, messages] of Object.entries(
    errors ?? {},
  )) {
    if (!Array.isArray(messages)) {
      continue
    }

    const safeMessages = messages.filter(
      (message): message is string =>
        typeof message === 'string',
    )

    const name = key
      .toLowerCase()
      .replace(/[^a-z]/g, '')

    if (name.endsWith('userid')) {
      result.userId = safeMessages
    } else if (
      name.endsWith('amount') ||
      name.endsWith('amountincents')
    ) {
      result.amount = safeMessages
    }
  }

  return result
}

export function describeContributionFailure(
  error: ApiError,
): ContributionFailure {
  const reference =
    error.problem?.correlationId ?? null

  const base = {
    reference,
    fieldErrors: {},
  }

  if (error.status === 0 || error.status >= 500) {
    return {
      ...base,
      status: 'uncertain',
      message:
        'The server could not confirm the result. Your contribution may have been recorded. Retry with the same details to check safely.',
    }
  }

  if (error.status === 400) {
    return {
      status: 'error',
      message:
        'Please correct the highlighted information.',
      reference,
      fieldErrors: contributionFieldErrors(
        error.problem?.errors,
      ),
    }
  }

  if (error.status === 403) {
    return {
      ...base,
      status: 'error',
      message:
        'You do not have permission to record this contribution.',
    }
  }

  if (error.status === 404) {
    return {
      ...base,
      status: 'error',
      message:
        'This stokvel or contribution cycle was not found.',
    }
  }

  if (
    error.status === 409 ||
    error.status === 422
  ) {
    return {
      ...base,
      status: 'error',
      message:
        error.problem?.detail ??
        'The contribution could not be recorded.',
    }
  }

  if (error.status === 429) {
    const seconds = parseRetryAfter(error.retryAfter)

    return {
      ...base,
      status: 'error',
      message:
        seconds === null
          ? 'Too many requests. Wait before trying again.'
          : `Too many requests. Try again in ${seconds} seconds.`,
    }
  }

  return {
    ...base,
    status: 'error',
    message:
      'The contribution could not be recorded. Please check the details and try again.',
  }
}
