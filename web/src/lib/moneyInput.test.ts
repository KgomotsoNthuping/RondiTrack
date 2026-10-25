
import { describe, expect, it } from 'vitest'

import {
  centsToInput,
  parseRandsToCents,
} from './money'

describe('parseRandsToCents', () => {
  it('converts rands to exact integer cents', () => {
    expect(parseRandsToCents('150.50')).toBe(15050)
    expect(parseRandsToCents('150.5')).toBe(15050)
    expect(parseRandsToCents('19.99')).toBe(1999)
    expect(parseRandsToCents('1')).toBe(100)
    expect(parseRandsToCents('0')).toBe(0)
  })

  it('accepts South African decimal formatting', () => {
    expect(parseRandsToCents('1 500,50')).toBe(150050)
    expect(parseRandsToCents('150,50')).toBe(15050)
    expect(parseRandsToCents('1 500')).toBe(150000)
  })

  it('rejects values requiring rounding', () => {
    expect(parseRandsToCents('150.505')).toBeNull()
    expect(parseRandsToCents('1.999')).toBeNull()
  })

  it('rejects invalid or ambiguous inputs', () => {
    expect(parseRandsToCents('')).toBeNull()
    expect(parseRandsToCents('-50')).toBeNull()
    expect(parseRandsToCents('abc')).toBeNull()
    expect(parseRandsToCents('1,500.50')).toBeNull()
    expect(parseRandsToCents('12 34.50')).toBeNull()
    expect(parseRandsToCents('R150')).toBeNull()
  })

  it('rejects amounts outside the safe integer range', () => {
    expect(
      parseRandsToCents('999999999999999999999'),
    ).toBeNull()
  })
})

describe('centsToInput', () => {
  it('produces editable amount text', () => {
    expect(centsToInput(15050)).toBe('150.50')
    expect(centsToInput(1999)).toBe('19.99')
    expect(centsToInput(100)).toBe('1.00')
    expect(centsToInput(0)).toBe('0.00')
  })

  it('rejects invalid cent values', () => {
    expect(() => centsToInput(-1)).toThrow()
    expect(() => centsToInput(1.5)).toThrow()
    expect(() => centsToInput(Number.NaN)).toThrow()
  })

  it('round-trips every amount from 0 to 100000 cents', () => {
    for (let cents = 0; cents <= 100000; cents++) {
      const input = centsToInput(cents)

      expect(parseRandsToCents(input)).toBe(cents)
    }
  })
})
