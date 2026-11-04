import 'server-only'

import { redirect } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
import { getApiBaseUrl } from './api'
import { getSession } from './session'
import { isInviteCode, joinHref, type JoinStep } from './joinFlow'
import {
  invitePreviewSchema,
  type InvitePreview,
} from './inviteTypes'

export type InviteLookup =
  | { kind: 'ready'; preview: InvitePreview }
  | { kind: 'unknown' }
  | { kind: 'expired' }
  | { kind: 'rate-limited'; retryAfter: string | null }
  | { kind: 'unavailable'; reference: string | null }

function loginDestination(
  code: string,
  step: JoinStep,
  expired: boolean,
): string {
  const query = new URLSearchParams({
    next: joinHref(code, step),
  })

  if (expired) {
    query.set('expired', '1')
  }

  return `/login?${query.toString()}`
}

export async function getInvitePreview(
  code: string,
  step: JoinStep,
): Promise<InviteLookup> {
  const session = await getSession()

  if (!session) {
    redirect(loginDestination(code, step, false))
  }

  if (!isInviteCode(code)) {
    return { kind: 'unknown' }
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/invites/${encodeURIComponent(code)}`,
      {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )
  } catch {
    return {
      kind: 'unavailable',
      reference: null,
    }
  }

  if (response.status === 401) {
    redirect(loginDestination(code, step, true))
  }

  if (response.status === 404) {
    return { kind: 'unknown' }
  }

  if (response.status === 410) {
    return { kind: 'expired' }
  }

  if (response.status === 429) {
    return {
      kind: 'rate-limited',
      retryAfter: response.headers.get('Retry-After'),
    }
  }

  if (!response.ok) {
    const error = await ApiError.fromResponse(response)

    return {
      kind: 'unavailable',
      reference: error.problem?.correlationId ?? null,
    }
  }

  try {
    const value: unknown = await response.json()
    const parsed = invitePreviewSchema.safeParse(value)

    if (!parsed.success) {
      return {
        kind: 'unavailable',
        reference: null,
      }
    }

    return {
      kind: 'ready',
      preview: parsed.data,
    }
  } catch {
    return {
      kind: 'unavailable',
      reference: null,
    }
  }
}
