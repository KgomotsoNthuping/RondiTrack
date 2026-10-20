
import { describe, expect, it } from 'vitest'
import { isTreasurerRole, roleLabel } from './roles'

describe('Stokvel roles', () => {
  it('recognizes the Treasurer role', () => {
    expect(isTreasurerRole('Treasurer')).toBe(true)
  })

  it('does not give Members treasurer access', () => {
    expect(isTreasurerRole('Member')).toBe(false)
  })

  it('does not accept a misspelled role', () => {
    expect(isTreasurerRole('Treasuer')).toBe(false)
  })

  it('displays each role as readable text', () => {
    expect(roleLabel('Treasurer'))
      .toBe("You're the Treasurer")

    expect(roleLabel('Member'))
      .toBe("You're a Member")

    expect(roleLabel('Admin'))
      .toBe("You're an Admin")
  })
})
