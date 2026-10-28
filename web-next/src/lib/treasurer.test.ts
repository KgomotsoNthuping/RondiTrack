import { describe, expect, it } from 'vitest'
import type { MeHomeEntry } from '../api/types'
import { stokvelsWhereTreasurer } from './treasurer'

function makeStokvel(
  id: string,
  role: string,
): MeHomeEntry {
  return {
    stokvelId: id,
    name: `Stokvel ${id}`,
    role,
    currentCycle: 1,
    cycleStatus: 'Open',
    cycleDueDate: null,
    paidThisCycleInCents: 10000,
    stillOwedInCents: 5000,
    nextPayoutDateUtc: null,
    arrearsInCents: 0,
  }
}

describe('stokvelsWhereTreasurer', () => {
  it('returns only Treasurer stokvels from mixed roles', () => {
    const stokvels = [
      makeStokvel('one', 'Treasurer'),
      makeStokvel('two', 'Member'),
      makeStokvel('three', 'Admin'),
      makeStokvel('four', 'Treasurer'),
    ]

    const result = stokvelsWhereTreasurer(stokvels)

    expect(result.map(item => item.stokvelId)).toEqual([
      'one',
      'four',
    ])
  })

  it('returns an empty list when the user is Treasurer nowhere', () => {
    const stokvels = [
      makeStokvel('one', 'Member'),
      makeStokvel('two', 'Admin'),
    ]

    expect(stokvelsWhereTreasurer(stokvels)).toEqual([])
  })

  it('returns an empty list for no memberships', () => {
    expect(stokvelsWhereTreasurer([])).toEqual([])
  })

  it('does not change the original list', () => {
    const stokvels = [
      makeStokvel('one', 'Treasurer'),
      makeStokvel('two', 'Member'),
    ]

    stokvelsWhereTreasurer(stokvels)

    expect(stokvels).toHaveLength(2)
    expect(stokvels[0].role).toBe('Treasurer')
    expect(stokvels[1].role).toBe('Member')
  })

  it('does not confuse unrelated role names with Treasurer', () => {
    const stokvels = [
      makeStokvel('one', 'AssistantTreasurer'),
      makeStokvel('two', 'Member'),
    ]

    expect(stokvelsWhereTreasurer(stokvels)).toEqual([])
  })
})
