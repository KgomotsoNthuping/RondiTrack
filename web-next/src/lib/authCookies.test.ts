import { describe, expect, it } from 'vitest'
import { extractRefreshToken } from './authCookies'

describe('refresh token cookie extraction', () => {
  it('reads the refresh token from Set-Cookie', () => {
    const value = extractRefreshToken([
      'refresh_token=abc123; Path=/api/v1/auth; HttpOnly; Secure',
    ])

    expect(value).toBe('abc123')
  })

  it('finds the token among multiple cookies', () => {
    const value = extractRefreshToken([
      'tracking=example; Path=/',
      'refresh_token=token456; Path=/api/v1/auth; HttpOnly',
    ])

    expect(value).toBe('token456')
  })

  it('does not mistake another cookie for the token', () => {
    expect(
      extractRefreshToken([
        'other_refresh_token=abc123; Path=/',
      ]),
    ).toBeNull()
  })

  it('returns null when no refresh cookie exists', () => {
    expect(extractRefreshToken([])).toBeNull()
  })
})
