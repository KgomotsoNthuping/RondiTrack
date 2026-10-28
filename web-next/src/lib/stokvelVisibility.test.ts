import { describe, expect, it } from 'vitest'
import { shouldHideStokvel } from './stokvelVisibility'

describe('stokvel access visibility', () => {
  it('hides a stokvel when the API returns 403', () => {
    expect(shouldHideStokvel(403)).toBe(true)
  })

  it('hides a nonexistent stokvel when the API returns 404', () => {
    expect(shouldHideStokvel(404)).toBe(true)
  })

  it('does not hide a successful response', () => {
    expect(shouldHideStokvel(200)).toBe(false)
  })

  it('does not disguise an authentication failure', () => {
    expect(shouldHideStokvel(401)).toBe(false)
  })

  it('does not disguise a server failure', () => {
    expect(shouldHideStokvel(500)).toBe(false)
  })
})
