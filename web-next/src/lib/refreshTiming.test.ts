
import { describe, expect, it } from 'vitest'
import {
  isAccessTokenExpired,
  needsRefresh,
} from './refreshTiming'

const NOW = Date.parse(
  '2026-10-29T10:00:00Z',
)

function expiresIn(
  seconds: number,
): string {
  return new Date(
    NOW + seconds * 1000,
  ).toISOString()
}

describe('refresh timing', () => {
  it('does not refresh when sufficient time remains', () => {
    expect(
      needsRefresh(expiresIn(120), NOW, 60_000),
    ).toBe(false)
  })

  it('refreshes when 60 seconds remain', () => {
    expect(
      needsRefresh(expiresIn(60), NOW, 60_000),
    ).toBe(true)
  })

  it('refreshes when the token has expired', () => {
    expect(
      needsRefresh(expiresIn(-10), NOW, 60_000),
    ).toBe(true)

    expect(
      isAccessTokenExpired(
        expiresIn(-10),
        NOW,
      ),
    ).toBe(true)
  })

  it('treats an invalid expiry value as expired', () => {
    expect(
      needsRefresh('invalid-date', NOW),
    ).toBe(true)

    expect(
      isAccessTokenExpired(
        'invalid-date',
        NOW,
      ),
    ).toBe(true)
  })
})
