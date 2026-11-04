import { describe, expect, it } from 'vitest'
import { joinHref } from './joinFlow'
import { safeNext } from './safeNext'

const code = 'a'.repeat(43)

describe('invitation redirects', () => {
  it('preserves the code and confirmation step', () => {
    const destination = joinHref(code, 'confirm')

    expect(safeNext(destination)).toBe(destination)
  })

  it('rejects external redirect destinations', () => {
    expect(
      safeNext('//attacker.example/join'),
    ).toBeNull()
  })

  it('preserves rules navigation through sign-in', () => {
    const destination = joinHref(code, 'rules')

    const loginUrl = new URL(
      '/login',
      'https://ronditrack.example',
    )

    loginUrl.searchParams.set(
      'next',
      destination,
    )

    expect(
      loginUrl.searchParams.get('next'),
    ).toBe(destination)

    expect(safeNext(destination)).toBe(destination)
  })
})
