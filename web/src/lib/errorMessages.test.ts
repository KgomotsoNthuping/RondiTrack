
import { describe, expect, it } from 'vitest'

import { ApiError } from '../api/ApiError'
import {
  describeError,
  parseRetryAfter,
} from './errorMessages'

describe('parseRetryAfter', () => {
  it('reads a number of seconds', () => {
    expect(parseRetryAfter('23')).toBe(23)
  })

  it('reads an HTTP date', () => {
    const now = Date.parse('2026-10-21T12:00:00Z')

    const retryDate = new Date(
      now + 23_000,
    ).toUTCString()

    expect(parseRetryAfter(retryDate, now)).toBe(23)
  })

  it('returns zero for a date in the past', () => {
    const now = Date.parse('2026-10-21T12:00:00Z')

    expect(
      parseRetryAfter(
        'Wed, 21 Oct 2026 11:59:00 GMT',
        now,
      ),
    ).toBe(0)
  })

  it('rejects invalid values', () => {
    expect(parseRetryAfter(null)).toBeNull()
    expect(parseRetryAfter('')).toBeNull()
    expect(parseRetryAfter('nonsense')).toBeNull()
    expect(parseRetryAfter('-5')).toBeNull()
    expect(parseRetryAfter('1.5')).toBeNull()
  })
})

describe('describeError', () => {
  it('shows a 404 as stokvel not found', () => {
    const error = new ApiError(404, {
      title: 'Not Found',
      correlationId: 'TEST-404',
    })

    const result = describeError(error)

    expect(result.title).toBe('Stokvel not found')
    expect(result.reference).toBe('TEST-404')
    expect(result.canRetry).toBe(false)
  })

  it('includes the actual rate limit wait time', () => {
    const error = new ApiError(
      429,
      {
        title: 'Too Many Requests',
        correlationId: 'TEST-429',
      },
      '23',
    )

    const result = describeError(error)

    expect(result.retryAfterSeconds).toBe(23)
    expect(result.message).toContain('23 seconds')
    expect(result.reference).toBe('TEST-429')
    expect(result.canRetry).toBe(false)
  })

  it('does not expose internal server details', () => {
    const error = new ApiError(500, {
      title: 'Internal Server Error',
      detail: 'Sensitive database connection details',
      correlationId: 'TEST-500',
    })

    const result = describeError(error)

    expect(result.message).not.toContain(
      'Sensitive database connection details',
    )

    expect(result.reference).toBe('TEST-500')
  })

  it('handles a network failure without a fake reference', () => {
    const result = describeError(
      new TypeError('Failed to fetch'),
    )

    expect(result.title).toBe("Can't reach RondiTrack")
    expect(result.reference).toBeNull()
    expect(result.canRetry).toBe(true)
  })

  it('preserves validation field errors', () => {
    const error = new ApiError(400, {
      title: 'Validation failed',
      errors: {
        email: ['A valid email address is required.'],
      },
    })

    const result = describeError(error)

    expect(result.fieldErrors.email).toEqual([
      'A valid email address is required.',
    ])
  })
})
