import { describe, expect, it } from 'vitest'
import type { InvitePreview } from './inviteTypes'
import {
  isInviteCode,
  joinHref,
  parseJoinStep,
  transitionJoin,
} from './joinFlow'

const code = 'a'.repeat(43)

const preview: InvitePreview = {
  stokvelId: '123e4567-e89b-42d3-a456-426614174001',
  name: 'Family Stokvel',
  rules: 'Contribute every month.',
  contributionAmountInCents: 50000,
  currentPeriod: 1,
  totalPeriods: 12,
  nextContributionDueDate: '2026-11-30',
  memberCount: 5,
  alreadyMember: false,
}

describe('join flow', () => {
  it('starts on the code step', () => {
    expect(parseJoinStep(undefined)).toBe('code')
  })

  it('rejects unknown step values', () => {
    expect(parseJoinStep('finished')).toBeNull()
  })

  it('requires the expected invitation code format', () => {
    expect(isInviteCode(code)).toBe(true)
    expect(isInviteCode('banana')).toBe(false)
  })

  it('moves to rules after accepting a valid code', () => {
    const next = transitionJoin(
      { step: 'code', code },
      { type: 'CODE_ACCEPTED', preview },
    )

    expect(next.step).toBe('rules')
  })

  it('does not advance with an invalid code', () => {
    const next = transitionJoin(
      { step: 'code', code: 'wrong' },
      { type: 'CODE_ACCEPTED', preview },
    )

    expect(next.step).toBe('code')
  })

  it('does not advance when already a member', () => {
    const next = transitionJoin(
      { step: 'code', code },
      {
        type: 'CODE_ACCEPTED',
        preview: {
          ...preview,
          alreadyMember: true,
        },
      },
    )

    expect(next.step).toBe('code')
  })

  it('moves from rules to confirmation', () => {
    const next = transitionJoin(
      { step: 'rules', code, preview },
      { type: 'CONTINUE' },
    )

    expect(next.step).toBe('confirm')
  })

  it('preserves the code when going backwards', () => {
    const confirm = transitionJoin(
      { step: 'rules', code, preview },
      { type: 'CONTINUE' },
    )

    const rules = transitionJoin(
      confirm,
      { type: 'BACK' },
    )

    const first = transitionJoin(
      rules,
      { type: 'BACK' },
    )

    expect(first).toEqual({
      step: 'code',
      code,
    })
  })

  it('does not skip directly from code to confirm', () => {
    const next = transitionJoin(
      { step: 'code', code },
      { type: 'CONTINUE' },
    )

    expect(next.step).toBe('code')
  })

  it('preserves the invitation in its URL', () => {
    const url = joinHref(code, 'confirm')
    const parsed = new URL(
      url,
      'https://ronditrack.example',
    )

    expect(parsed.pathname).toBe('/join')
    expect(parsed.searchParams.get('code')).toBe(code)
    expect(parsed.searchParams.get('step')).toBe('confirm')
  })

  it('uses a clean URL for the first step', () => {
    expect(joinHref('', 'code')).toBe('/join')
  })
})
