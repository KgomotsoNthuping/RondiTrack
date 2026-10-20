import { describe, expect, it } from 'vitest'
import { contributionStatusLabel } from './contributionStatus'

describe('Contribution status', () => {
  it('recognizes all supported statuses', () => {
    expect(contributionStatusLabel('Pending')).toBe('Pending')
    expect(contributionStatusLabel('Paid')).toBe('Paid')
    expect(contributionStatusLabel('Confirmed')).toBe('Confirmed')
  })

  it('does not silently accept a typo', () => {
    expect(contributionStatusLabel('Piad'))
      .toBe('Unknown status')
  })
})
