import { describe, expect, it } from 'vitest'
import { ApiError } from '@/api/ApiError'
import { describeContributionFailure } from './contributionErrors'

describe('contribution error mapping', () => {
  it('places validation errors beside the amount field', () => {
    const error = new ApiError(400, {
      correlationId: 'request-400',
      errors: {
        AmountInCents: ['Amount must be positive.'],
      },
    })

    const result = describeContributionFailure(error)

    expect(result.status).toBe('error')
    expect(result.reference).toBe('request-400')
    expect(result.fieldErrors.amount).toEqual([
      'Amount must be positive.',
    ])
  })

  it('does not sign the user out on 403', () => {
    const result = describeContributionFailure(
      new ApiError(403, {
        correlationId: 'request-403',
      }),
    )

    expect(result.status).toBe('error')
    expect(result.message).toContain('permission')
    expect(result.reference).toBe('request-403')
  })

  it('shows the API detail for a conflict', () => {
    const result = describeContributionFailure(
      new ApiError(409, {
        detail: 'This contribution conflicts with an existing request.',
      }),
    )

    expect(result.message).toContain('conflicts')
  })

  it('shows retry delay for rate limiting', () => {
    const result = describeContributionFailure(
      new ApiError(429, null, '30'),
    )

    expect(result.message).toContain('30 seconds')
  })

  it('treats server failure as an uncertain payment outcome', () => {
    const result = describeContributionFailure(
      new ApiError(500, {
        detail: 'Private database information',
      }),
    )

    expect(result.status).toBe('uncertain')
    expect(result.message).toContain(
      'may have been recorded',
    )
    expect(result.message).not.toContain(
      'Private database information',
    )
  })
})
