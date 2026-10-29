import { describe, expect, it } from 'vitest'
import { safeNext } from './safeNext'

describe('safeNext', () => {
  it('accepts a local dashboard route', () => {
    expect(safeNext('/dashboard/member')).toBe(
      '/dashboard/member',
    )
  })

  it('accepts a stokvel detail route', () => {
    expect(safeNext('/stokvels/123')).toBe('/stokvels/123')
  })

  it('preserves query parameters', () => {
    expect(safeNext('/stokvels?page=2')).toBe(
      '/stokvels?page=2',
    )
  })

  it('rejects a protocol-relative URL', () => {
    expect(safeNext('//evil.example')).toBeNull()
  })

  it('rejects a backslash-based redirect', () => {
    expect(safeNext('/\\evil.example')).toBeNull()
  })

  it('rejects an absolute external URL', () => {
    expect(
      safeNext('https://evil.example'),
    ).toBeNull()
  })

  it('rejects encoded path separators', () => {
    expect(safeNext('/%2Fevil.example')).toBeNull()
    expect(safeNext('/%5Cevil.example')).toBeNull()
  })

  it('rejects an auth-page redirect loop', () => {
    expect(safeNext('/login')).toBeNull()
    expect(safeNext('/register')).toBeNull()
  })

  it('rejects missing or invalid values', () => {
    expect(safeNext('')).toBeNull()
    expect(safeNext(null)).toBeNull()
    expect(safeNext(undefined)).toBeNull()
    expect(safeNext(' dashboard/member')).toBeNull()
  })
})
