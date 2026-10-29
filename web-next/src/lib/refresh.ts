import 'server-only'

import { createHash } from 'node:crypto'
import { extractRefreshToken } from '@/lib/authCookies'
import { createSingleFlight } from '@/lib/singleFlight'
import type { SessionData } from '@/lib/session'

export type RefreshOutcome =
  | {
      kind: 'ok'
      session: SessionData
    }
  | {
      kind: 'refused'
    }
  | {
      kind: 'temporary'
    }

const singleFlight = createSingleFlight<RefreshOutcome>(
  10_000,
)

type RefreshResponse = {
  accessToken: string
  expiresAtUtc: string
}

function isRefreshResponse(
  value: unknown,
): value is RefreshResponse {
  if (
    value === null ||
    typeof value !== 'object' ||
    Array.isArray(value)
  ) {
    return false
  }

  const data = value as Record<string, unknown>

  return (
    typeof data.accessToken === 'string' &&
    data.accessToken.length > 0 &&
    typeof data.expiresAtUtc === 'string' &&
    Number.isFinite(
      Date.parse(data.expiresAtUtc),
    )
  )
}

async function performRefresh(
  session: SessionData,
): Promise<RefreshOutcome> {
  const baseUrl = process.env.API_BASE_URL
    ?.trim()
    .replace(/\/$/, '')

  if (!baseUrl) {
    console.error(
      '[RondiTrack] API_BASE_URL is not configured.',
    )

    return { kind: 'temporary' }
  }

  try {
    const response = await fetch(
      `${baseUrl}/api/v1/auth/refresh`,
      {
        method: 'POST',
        headers: {
          Cookie:
            `refresh_token=${encodeURIComponent(
              session.refreshToken,
            )}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )

    
    if (
      response.status === 400 ||
      response.status === 401 ||
      response.status === 403
    ) {
      console.info(
        '[RondiTrack] Refresh token refused.',
      )

      return { kind: 'refused' }
    }

    /*
     * Network or backend problems should
     * not be mistaken for token revocation.
     */
    if (!response.ok) {
      console.error(
        '[RondiTrack] Refresh temporarily failed:',
        response.status,
      )

      return { kind: 'temporary' }
    }

    const data: unknown = await response.json()

    const newRefreshToken = extractRefreshToken(
      response.headers.getSetCookie(),
    )

    if (
      !isRefreshResponse(data) ||
      !newRefreshToken
    ) {
      console.error(
        '[RondiTrack] Invalid refresh response.',
      )

      return { kind: 'temporary' }
    }

    if (
      Date.parse(data.expiresAtUtc) <= Date.now()
    ) {
      console.error(
        '[RondiTrack] API returned an expired access token.',
      )

      return { kind: 'temporary' }
    }

    console.info(
      '[RondiTrack] Session refreshed at',
      new Date().toISOString(),
    )

    return {
      kind: 'ok',
      session: {
        ...session,
        accessToken: data.accessToken,
        refreshToken: newRefreshToken,
        accessTokenExpiresAtUtc:
          data.expiresAtUtc,
      },
    }
  } catch {
    console.error(
      '[RondiTrack] Could not contact refresh endpoint.',
    )

    return { kind: 'temporary' }
  }
}

export async function refreshOnce(
  session: SessionData,
): Promise<RefreshOutcome> {
  const key = createHash('sha256')
    .update(session.refreshToken, 'utf8')
    .digest('hex')

  try {
    return await singleFlight(key, async () => {
      const outcome = await performRefresh(session)

      if (outcome.kind === 'temporary') {
        throw new Error(
          'Temporary refresh failure.',
        )
      }

      return outcome
    })
  } catch {
    return { kind: 'temporary' }
  }
}
