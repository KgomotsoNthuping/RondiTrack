import { describe, expect, it } from 'vitest'
import { formatCents } from './money'

const NBSP = String.fromCharCode(0xa0)

describe('formatCents', () => {
  it('formats ten cents', () => {
    expect(formatCents(10)).toBe(`R${NBSP}0,10`)
  })

  it('formats zero cents', () => {
    expect(formatCents(0)).toBe(`R${NBSP}0,00`)
  })

  it('formats R150,50', () => {
    expect(formatCents(15050))
      .toBe(`R${NBSP}150,50`)
  })

  it('formats thousands correctly', () => {
    expect(formatCents(1234567))
      .toBe(`R${NBSP}12${NBSP}345,67`)
  })

  it('rejects fractional cents', () => {
    expect(() => formatCents(10.5))
      .toThrow(RangeError)
  })

  it('formats negative amounts', () => {
    expect(formatCents(-150))
      .toBe(`-R${NBSP}1,50`)
  })

  it('rejects unsafe integers', () => {
    expect(() =>
      formatCents(Number.MAX_SAFE_INTEGER + 1),
    ).toThrow(RangeError)
  })
})
