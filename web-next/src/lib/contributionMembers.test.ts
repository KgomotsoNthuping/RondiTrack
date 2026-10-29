import { describe, expect, it } from 'vitest'
import {
  canSelectContributionMember,
  toRecordableMembers,
} from './contributionMembers'

describe('contribution member permissions', () => {
  it('allows a Treasurer to select other members', () => {
    expect(
      canSelectContributionMember('Treasurer'),
    ).toBe(true)
  })

  it('does not show the picker to a Member', () => {
    expect(
      canSelectContributionMember('Member'),
    ).toBe(false)
  })

  it('does not show the Treasurer picker to an Admin', () => {
    expect(
      canSelectContributionMember('Admin'),
    ).toBe(false)
  })

  it('rejects missing or unrelated roles', () => {
    expect(
      canSelectContributionMember(null),
    ).toBe(false)

    expect(
      canSelectContributionMember(undefined),
    ).toBe(false)

    expect(
      canSelectContributionMember('AssistantTreasurer'),
    ).toBe(false)
  })

  it('checks each stokvel role separately', () => {
    const familyStokvel = {
      role: 'Treasurer',
    }

    const workStokvel = {
      role: 'Member',
    }

    expect(
      canSelectContributionMember(
        familyStokvel.role,
      ),
    ).toBe(true)

    expect(
      canSelectContributionMember(
        workStokvel.role,
      ),
    ).toBe(false)
  })

  it('passes only the member ID and name to the form', () => {
    const result = toRecordableMembers([
      {
        id: '123e4567-e89b-42d3-a456-426614174001',
        fullName: 'Test Member',
        email: 'private@example.com',
        phoneNumber: '0712345678',
        version: 1,
      },
    ])

    expect(result).toEqual([
      {
        id: '123e4567-e89b-42d3-a456-426614174001',
        fullName: 'Test Member',
      },
    ])

    expect(JSON.stringify(result)).not.toContain(
      'private@example.com',
    )
  })
})
