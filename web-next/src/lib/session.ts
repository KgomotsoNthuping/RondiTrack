import 'server-only'
import { cookies } from 'next/headers'
import { EncryptJWT, jwtDecrypt } from 'jose'

const COOKIE_NAME = 'session'
const SESSION_MAX_AGE = 7 * 24 * 60 * 60

export type SessionData = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  userId: string
  email: string
  fullName: string
}

function sessionKey(): Uint8Array {
  const secret = process.env.SESSION_SECRET

  if (!secret) {
    throw new Error('SESSION_SECRET is not configured.')
  }

  const bytes = Buffer.from(secret, 'base64url')

  if (bytes.length !== 32) {
    throw new Error('SESSION_SECRET must contain 32 random bytes.')
  }

  return new Uint8Array(bytes)
}

function isSessionData(value: unknown): value is SessionData {
  if (
    value === null ||
    typeof value !== 'object' ||
    Array.isArray(value)
  ) {
    return false
  }

  const data = value as Record<string, unknown>

  const requiredFields = [
    'accessToken',
    'refreshToken',
    'accessTokenExpiresAtUtc',
    'userId',
    'email',
    'fullName',
  ] as const

  if (
    requiredFields.some(
      field =>
        typeof data[field] !== 'string' ||
        (data[field] as string).trim().length === 0,
    )
  ) {
    return false
  }

  if ('role' in data) {
    return false
  }

  return !Number.isNaN(
    Date.parse(data.accessTokenExpiresAtUtc as string),
  )
}

export async function createSession(
  data: SessionData,
): Promise<void> {
  if (!isSessionData(data)) {
    throw new Error('Invalid session data.')
  }

  const encrypted = await new EncryptJWT({ ...data })
    .setProtectedHeader({
      alg: 'dir',
      enc: 'A256GCM',
    })
    .setIssuedAt()
    .setExpirationTime('7d')
    .encrypt(sessionKey())

  const store = await cookies()

  store.set(COOKIE_NAME, encrypted, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: '/',
    maxAge: SESSION_MAX_AGE,
  })
}

export async function getSession(): Promise<SessionData | null> {
  const store = await cookies()
  const encrypted = store.get(COOKIE_NAME)?.value

  if (!encrypted) {
    return null
  }

  try {
    const { payload } = await jwtDecrypt(
      encrypted,
      sessionKey(),
      {
        keyManagementAlgorithms: ['dir'],
        contentEncryptionAlgorithms: ['A256GCM'],
      },
    )

    if (!isSessionData(payload)) {
      return null
    }

    return {
      accessToken: payload.accessToken,
      refreshToken: payload.refreshToken,
      accessTokenExpiresAtUtc:
        payload.accessTokenExpiresAtUtc,
      userId: payload.userId,
      email: payload.email,
      fullName: payload.fullName,
    }
  } catch {
    return null
  }
}

export async function deleteSession(): Promise<void> {
  const store = await cookies()

  store.delete(COOKIE_NAME)
}
