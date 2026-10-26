import { describe, expect, it } from 'vitest'
import {
  contributionSchema,
  toContributionRequest,
} from './contribution'

const memberId = '123e4567-e89b-42d3-a456-426614174000'

describe('contributionSchema', () => {
  it('accepts a valid contribution', () => {
    const result = contributionSchema.safeParse({
      userId: memberId,
      amount: '150.50',
    })

    expect(result.success).toBe(true)
  })

  it('rejects an empty amount', () => {
    const result = contributionSchema.safeParse({
      userId: memberId,
      amount: '',
    })

    expect(result.success).toBe(false)
  })

  it('rejects more than two decimal places', () => {
    const result = contributionSchema.safeParse({
      userId: memberId,
      amount: '150.505',
    })

    expect(result.success).toBe(false)
  })

  it('rejects a missing member', () => {
    const result = contributionSchema.safeParse({
      userId: '',
      amount: '150.50',
    })

    expect(result.success).toBe(false)
  })

  it('converts a valid form value to integer cents', () => {
    const values = contributionSchema.parse({
      userId: memberId,
      amount: '1 500,50',
    })

    expect(toContributionRequest(values)).toEqual({
      userId: memberId,
      amountInCents: 150050,
    })
  })
})