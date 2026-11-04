'use server'

import { refresh } from 'next/cache'
import { redirect } from 'next/navigation'

import { ApiError } from '@/api/ApiError'
import { getApiBaseUrl } from '@/lib/api'
import { isInviteCode, joinHref } from '@/lib/joinFlow'
import {
  createInviteResponseSchema,
  invitePreviewSchema,
  joinResponseSchema,
} from '@/lib/inviteTypes'
import { getSession } from '@/lib/session'

export type JoinActionState = {
  status:
    | 'idle'
    | 'joined'
    | 'already-member'
    | 'unknown'
    | 'expired'
    | 'rate-limited'
    | 'error'
    | 'uncertain'
  message: string | null
  stokvelId: string | null
  reference: string | null
}

export type CreateInviteState = {
  status: 'idle' | 'created' | 'error'
  code: string | null
  expiresAtUtc: string | null
  message: string | null
  reference: string | null
}

export async function joinWithInviteAction(
  _previous: JoinActionState,
  formData: FormData,
): Promise<JoinActionState> {
  const rawCode = formData.get('code')
  const code = typeof rawCode === 'string'
    ? rawCode.trim()
    : ''

  if (!isInviteCode(code)) {
    return {
      status: 'unknown',
      message: 'Check the invitation code and try again.',
      stokvelId: null,
      reference: null,
    }
  }

  const session = await getSession()

  if (!session) {
    const query = new URLSearchParams({
      next: joinHref(code, 'confirm'),
    })

    redirect(`/login?${query.toString()}`)
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/invites/${encodeURIComponent(code)}/join`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )
  } catch {
    return {
      status: 'uncertain',
      message:
        'The connection was interrupted. Your membership may already have been created. Open the invitation again to check before retrying.',
      stokvelId: null,
      reference: null,
    }
  }

  if (response.status === 401) {
    const query = new URLSearchParams({
      next: joinHref(code, 'confirm'),
      expired: '1',
    })

    redirect(`/login?${query.toString()}`)
  }

  if (response.ok) {
    try {
      const value: unknown = await response.json()
      const parsed = joinResponseSchema.safeParse(value)

      if (!parsed.success) {
        return {
          status: 'uncertain',
          message:
            'The API accepted the request but the confirmation could not be read. Check your stokvels before trying again.',
          stokvelId: null,
          reference: null,
        }
      }

      refresh()

      return {
        status: 'joined',
        message: `You have joined ${parsed.data.name}.`,
        stokvelId: parsed.data.stokvelId,
        reference: null,
      }
    } catch {
      return {
        status: 'uncertain',
        message:
          'The membership result could not be confirmed. Check your stokvels before trying again.',
        stokvelId: null,
        reference: null,
      }
    }
  }

  if (response.status === 409) {
    try {
      const check = await fetch(
        `${getApiBaseUrl()}/api/v1/invites/${encodeURIComponent(code)}`,
        {
          headers: {
            Authorization: `Bearer ${session.accessToken}`,
            Accept: 'application/json',
          },
          cache: 'no-store',
        },
      )

      if (check.ok) {
        const value: unknown = await check.json()
        const parsed = invitePreviewSchema.safeParse(value)

        if (parsed.success && parsed.data.alreadyMember) {
          refresh()

          return {
            status: 'already-member',
            message:
              'You are already a member of this stokvel.',
            stokvelId: parsed.data.stokvelId,
            reference: null,
          }
        }
      }
    } catch {
      return {
        status: 'error',
        message:
          'The membership could not be verified. Check My stokvels before trying again.',
        stokvelId: null,
        reference: null,
      }
    }

    return {
      status: 'error',
      message:
        'The invitation cannot be completed because of a conflict.',
      stokvelId: null,
      reference: null,
    }
  }

  if (response.status === 404 || response.status === 410) {
    return {
      status: response.status === 404 ? 'unknown' : 'expired',
      message:
        response.status === 404
          ? 'Check the invitation code and try again.'
          : 'This invitation has expired. Ask for a new one.',
      stokvelId: null,
      reference: null,
    }
  }

  if (response.status === 429) {
    return {
      status: 'rate-limited',
      message:
        'Too many attempts. Try again later.',
      stokvelId: null,
      reference: null,
    }
  }

  const error = await ApiError.fromResponse(response)

  return {
    status: 'error',
    message:
      response.status === 403
        ? 'You do not have permission to join using this invitation.'
        : 'The invitation could not be completed. Please try again.',
    stokvelId: null,
    reference: error.problem?.correlationId ?? null,
  }
}

export async function createStokvelInviteAction(
  _previous: CreateInviteState,
  formData: FormData,
): Promise<CreateInviteState> {
  const rawId = formData.get('stokvelId')

  const stokvelId =
    typeof rawId === 'string' ? rawId : ''

  if (
    !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(stokvelId)
  ) {
    return {
      status: 'error',
      code: null,
      expiresAtUtc: null,
      message: 'Invalid stokvel ID.',
      reference: null,
    }
  }

  const session = await getSession()

  if (!session) {
    redirect('/login')
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/stokvels/${stokvelId}/invites`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )
  } catch {
    return {
      status: 'error',
      code: null,
      expiresAtUtc: null,
      message: 'Could not contact the invitation service.',
      reference: null,
    }
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (!response.ok) {
    const error = await ApiError.fromResponse(response)

    return {
      status: 'error',
      code: null,
      expiresAtUtc: null,
      message:
        response.status === 403
          ? 'Only this stokvel’s Treasurer may create invitations.'
          : response.status === 429
            ? 'Too many invitations requested. Try again later.'
            : 'Could not create the invitation.',
      reference: error.problem?.correlationId ?? null,
    }
  }

  try {
    const value: unknown = await response.json()
    const parsed = createInviteResponseSchema.safeParse(value)

    if (!parsed.success || !isInviteCode(parsed.data.code)) {
      throw new Error('Invalid invitation response.')
    }

    return {
      status: 'created',
      code: parsed.data.code,
      expiresAtUtc: parsed.data.expiresAtUtc,
      message: 'Invitation created successfully.',
      reference: null,
    }
  } catch {
    return {
      status: 'error',
      code: null,
      expiresAtUtc: null,
      message:
        'The response could not be confirmed. You can request another invitation.',
      reference: null,
    }
  }
}
