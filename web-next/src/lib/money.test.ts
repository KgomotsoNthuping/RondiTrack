
import { describe, expect, it } from 'vitest'
import {
  centsToInput,
  formatCents,
  parseRandsToCents,
} from './money'

describe('South African money parsing', () => {
  it.each([
    ['1500', 150000],
    ['1 500', 150000],
    ['1 500,50', 150050],
    ['R1500', 150000],
    ['R 1 500,50', 150050],
    ['R\u00a01\u00a0500,50', 150050],
    ['R\u202f1\u202f500,50', 150050],
    ['1,5', 150],
    [',5', 50],
    ['19,99', 1999],
    ['19.99', 1999],
    ['0', 0],
  ])('parses %s as %i cents', (input, expected) => {
    expect(parseRandsToCents(input)).toBe(expected)
  })

  it.each([
    '',
    ' ',
    '19,999',
    '1,500.50',
    'R -500',
    '-500',
    'hello',
    'Rabc',
    '1 50',
    '90071992547409,92',
  ])('rejects ambiguous or invalid input %s', input => {
    expect(parseRandsToCents(input)).toBeNull()
  })

  it.each([
    0,
    1,
    99,
    100,
    150050,
    Number.MAX_SAFE_INTEGER,
  ])('round-trips %i cents through the formatter', cents => {
    expect(
      parseRandsToCents(formatCents(cents)),
    ).toBe(cents)
  })

  it('converts cents to an editable amount', () => {
    expect(centsToInput(150050)).toBe('1500.50')
  })

  it('rejects invalid cent values', () => {
    expect(() => centsToInput(-1)).toThrow(RangeError)
    expect(() => centsToInput(1.5)).toThrow(RangeError)
    expect(() => formatCents(Number.NaN)).toThrow(RangeError)
  })
})
