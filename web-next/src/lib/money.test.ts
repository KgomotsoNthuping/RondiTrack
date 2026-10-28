import { describe, expect, it } from 'vitest'
import {
  centsToInput,
  parseRandsToCents,
} from './money'

describe('money conversion', () => {
  it('converts rand input into integer cents', () => {
    expect(parseRandsToCents('150.50')).toBe(15050)
  })

  it('rejects more than two decimal places', () => {
    expect(parseRandsToCents('150.505')).toBeNull()
  })

  it('converts integer cents into form input', () => {
    expect(centsToInput(15050)).toBe('150.50')
  })
})