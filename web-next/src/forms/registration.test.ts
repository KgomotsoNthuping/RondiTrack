import { describe, expect, it } from 'vitest'
import { registrationSchema } from './registration'

const valid = {
  fullName: 'Test Member',
  email: 'member@example.com',
  phoneNumber: '0712345678',
  password: 'ExamplePass123!',
  confirmPassword: 'ExamplePass123!',
}

describe('registrationSchema', () => {
  it('accepts valid registration details', () => {
    expect(
      registrationSchema.safeParse(valid).success,
    ).toBe(true)
  })

  it('rejects a password shorter than 12 characters', () => {
    expect(
      registrationSchema.safeParse({
        ...valid,
        password: 'Short123',
        confirmPassword: 'Short123',
      }).success,
    ).toBe(false)
  })

  it('rejects mismatched passwords', () => {
    const result = registrationSchema.safeParse({
      ...valid,
      confirmPassword: 'DifferentPass123!',
    })

    expect(result.success).toBe(false)

    if (!result.success) {
      expect(
        result.error.issues.some(
          issue =>
            issue.path[0] === 'confirmPassword',
        ),
      ).toBe(true)
    }
  })

  it('rejects an invalid email', () => {
    expect(
      registrationSchema.safeParse({
        ...valid,
        email: 'not-an-email',
      }).success,
    ).toBe(false)
  })

  it('rejects a missing phone number', () => {
    expect(
      registrationSchema.safeParse({
        ...valid,
        phoneNumber: '',
      }).success,
    ).toBe(false)
  })

  it('rejects an empty full name', () => {
    expect(
      registrationSchema.safeParse({
        ...valid,
        fullName: ' ',
      }).success,
    ).toBe(false)
  })
})
