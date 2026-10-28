import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { describeError } from './errorMessages'

describe('API error descriptions', () => {
  it('shows the expected message for a 403', () => {
    const error = new ApiError(403, {
      title: 'Forbidden',
      correlationId: 'request-403',
    })

    const result = describeError(error)

    expect(result.title).toBe('Access denied')
    expect(result.reference).toBe('request-403')
    expect(result.canRetry).toBe(false)
  })

  it('uses the same user-facing not-found message for a 404', () => {
    const error = new ApiError(404, {
      correlationId: 'request-404',
    })

    const result = describeError(error)

    expect(result.title).toBe('Stokvel not found')
    expect(result.reference).toBe('request-404')
    expect(result.message).toContain(
      'may no longer exist',
    )
  })

  it('does not expose internal details from a 500', () => {
    const error = new ApiError(500, {
      title: 'Internal Server Error',
      detail: 'Sensitive internal database information',
      correlationId: 'request-500',
    })

    const result = describeError(error)

    expect(result.title).toBe('Something went wrong')
    expect(result.reference).toBe('request-500')
    expect(result.canRetry).toBe(true)
    expect(result.message).not.toContain(
      'Sensitive internal database information',
    )
  })

  it('shows retry information for rate limiting', () => {
    const error = new ApiError(
      429,
      { correlationId: 'request-429' },
      '30',
    )

    const result = describeError(error)

    expect(result.retryAfterSeconds).toBe(30)
    expect(result.message).toContain('30 seconds')
    expect(result.canRetry).toBe(false)
  })

  it('does not disclose unexpected error messages', () => {
    const error = new Error('Private server detail')

    const result = describeError(error)

    expect(result.reference).toBeNull()
    expect(result.message).not.toContain(
      'Private server detail',
    )
  })
})
