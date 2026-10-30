
import { describe, expect, it } from 'vitest'

import {
  isValidCalendarDate,
  southAfricanDaysToUtcRange,
} from './southAfricanDates'

describe('South African contribution dates', () => {
  it('converts all of October 2026 to UTC', () => {
    expect(
      southAfricanDaysToUtcRange(
        '2026-10-01',
        '2026-10-31',
      ),
    ).toEqual({
      fromUtc: '2026-09-30T22:00:00Z',
      toUtc: '2026-10-31T22:00:00Z',
      isReversed: false,
    })
  })

  it('includes 23:30 SAST on 31 October', () => {
    const range = southAfricanDaysToUtcRange(
      '2026-10-01',
      '2026-10-31',
    )

    const payment = Date.parse(
      '2026-10-31T21:30:00Z',
    )

    expect(payment).toBeGreaterThanOrEqual(
      Date.parse(range.fromUtc!),
    )

    expect(payment).toBeLessThan(
      Date.parse(range.toUtc!),
    )
  })

  it('excludes midnight SAST on 1 November', () => {
    const range = southAfricanDaysToUtcRange(
      '2026-10-01',
      '2026-10-31',
    )

    const nextMonth = Date.parse(
      '2026-10-31T22:00:00Z',
    )

    expect(nextMonth).toBe(
      Date.parse(range.toUtc!),
    )
  })

  it('covers exactly 24 hours for a single day', () => {
    const range = southAfricanDaysToUtcRange(
      '2026-10-31',
      '2026-10-31',
    )

    expect(
      Date.parse(range.toUtc!) -
        Date.parse(range.fromUtc!),
    ).toBe(86_400_000)
  })

  it('supports a from-only range', () => {
    expect(
      southAfricanDaysToUtcRange(
        '2026-10-01',
        null,
      ),
    ).toEqual({
      fromUtc: '2026-09-30T22:00:00Z',
      toUtc: null,
      isReversed: false,
    })
  })

  it('supports a to-only range', () => {
    expect(
      southAfricanDaysToUtcRange(
        null,
        '2026-10-31',
      ),
    ).toEqual({
      fromUtc: null,
      toUtc: '2026-10-31T22:00:00Z',
      isReversed: false,
    })
  })

  it('ignores reversed date limits', () => {
    expect(
      southAfricanDaysToUtcRange(
        '2026-11-01',
        '2026-10-01',
      ),
    ).toEqual({
      fromUtc: null,
      toUtc: null,
      isReversed: true,
    })
  })

  it('rejects a nonexistent date', () => {
    expect(
      isValidCalendarDate('2026-02-30'),
    ).toBe(false)

    expect(() =>
      southAfricanDaysToUtcRange(
        '2026-02-30',
        null,
      ),
    ).toThrow(RangeError)
  })

  it('accepts leap day only in a leap year', () => {
    expect(
      isValidCalendarDate('2028-02-29'),
    ).toBe(true)

    expect(
      isValidCalendarDate('2026-02-29'),
    ).toBe(false)
  })

  it('rejects invalid date formats', () => {
    expect(
      isValidCalendarDate('2026-2-1'),
    ).toBe(false)

    expect(
      isValidCalendarDate('yesterday'),
    ).toBe(false)
  })
})
