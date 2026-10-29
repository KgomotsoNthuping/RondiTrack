import 'server-only'

import { cookies } from 'next/headers'
import { EncryptJWT, jwtDecrypt } from 'jose'

export const SESSION_COOKIE = 'session'

// Eight hours of inactivity.
// A successful token refresh renews this session.
export const SESSION_MAX_AGE = 8 * 60 * 60

export type SessionData = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  userId: string
  email: string
  fullName: string
}

export function sessionCookieOptions() {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax' as const,
    path: '/',
    maxAge: SESSION_MAX_AGE,
  }
}

function sessionKey(): Uint8Array {
  const secret = process.env.SESSION_SECRET

  if (!secret) {
    throw new Error('SESSION_SECRET is not configured.')
  }

  const bytes = Buffer.from(secret, 'base64url')

  if (bytes.length !== 32) {
    throw new Error(
      'SESSION_SECRET must contain 32 random bytes.',
    )
  }

  return new Uint8Array(bytes)
}

function isSessionData(
  value: unknown,
): value is SessionData {
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

  const validFields = requiredFields.every(
    field =>
      typeof data[field] === 'string' &&
      (data[field] as string).trim().length > 0,
  )

  if (!validFields) {
    return false
  }

  // Treasurer is a per-stokvel role.
  // A global role must not be stored in this session.
  if ('role' in data) {
    return false
  }

  const expiresAt = Date.parse(
    data.accessTokenExpiresAtUtc as string,
  )

  return Number.isFinite(expiresAt)
}

/**
 * Encrypt session data without accessing cookies.
 * Used by createSession() and proxy.ts.
 */
export async function sealSession(
  data: SessionData,
): Promise<string> {
  if (!isSessionData(data)) {
    throw new Error('Invalid session data.')
  }

  return new EncryptJWT({ ...data })
    .setProtectedHeader({
      alg: 'dir',
      enc: 'A256GCM',
    })
    .setIssuedAt()
    .setExpirationTime('8h')
    .encrypt(sessionKey())
}

/**
 * Decrypt session data supplied as cookie text.
 * Tampered, expired or invalid sessions return null.
 */
export async function openSession(
  encrypted: string | null | undefined,
): Promise<SessionData | null> {
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

/**
 * Used by the existing login Server Action.
 */
export async function createSession(
  data: SessionData,
): Promise<void> {
  const encrypted = await sealSession(data)
  const store = await cookies()

  store.set(
    SESSION_COOKIE,
    encrypted,
    sessionCookieOptions(),
  )
}

/**
 * Used by Server Components and Server Actions.
 * They read the session but never refresh it.
 */
export async function getSession(): Promise<SessionData | null> {
  const store = await cookies()

  return openSession(
    store.get(SESSION_COOKIE)?.value,
  )
}

/**
 * Used by the existing logout Server Action.
 */
export async function deleteSession(): Promise<void> {
  const store = await cookies()

  store.delete(SESSION_COOKIE)
}
