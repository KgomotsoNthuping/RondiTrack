import { describe, expect, it } from 'vitest'

import {
  formatDate,
  formatDateTime,
  formatCalendarDate,
} from './dates'

describe('South African date formatting', () => {
  it('handles the midnight payment correctly', () => {
    expect(
      formatDateTime('2026-10-31T22:30:00Z'),
    ).toBe('01 Nov 2026, 00:30')
  })

  it('formats a UTC date in Johannesburg', () => {
    expect(
      formatDate('2026-10-31T22:30:00Z'),
    ).toBe('01 Nov 2026')
  })

  it('formats an ordinary payment timestamp', () => {
    expect(
      formatDateTime('2026-10-15T12:00:00Z'),
    ).toBe('15 Oct 2026, 14:00')
  })

  it('preserves a date-only cycle due date', () => {
    expect(
      formatCalendarDate('2026-10-31'),
    ).toBe('31 Oct 2026')
  })

  it('rejects an invalid calendar date', () => {
    expect(() =>
      formatCalendarDate('2026-02-30'),
    ).toThrow(RangeError)
  })
})
