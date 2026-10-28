'use server'

import { redirect } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
import { extractRefreshToken } from '@/lib/authCookies'
import { describeError } from '@/lib/errorMessages'
import { getApiBaseUrl } from '@/lib/api'
import {
  createSession,
  deleteSession,
  getSession,
} from '@/lib/session'

export type LoginState = {
  message: string | null
  reference: string | null
  fieldErrors: Record<string, string[]>
}

type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
}

type MeResponse = {
  id: string
  email: string
  fullName: string
}

function isLoginResponse(
  value: unknown,
): value is LoginResponse {
  if (
    !value ||
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
    !Number.isNaN(Date.parse(data.expiresAtUtc))
  )
}

function isMeResponse(value: unknown): value is MeResponse {
  if (
    !value ||
    typeof value !== 'object' ||
    Array.isArray(value)
  ) {
    return false
  }

  const data = value as Record<string, unknown>

  return (
    typeof data.id === 'string' &&
    data.id.length > 0 &&
    typeof data.email === 'string' &&
    data.email.length > 0 &&
    typeof data.fullName === 'string' &&
    data.fullName.length > 0
  )
}

async function errorFromResponse(
  response: Response,
): Promise<ApiError> {
  return ApiError.fromResponse(response)
}

function loginFailure(error: unknown): LoginState {
  const description = describeError(error)

  return {
    message:
      error instanceof ApiError && error.status === 401
        ? 'Email or password is incorrect.'
        : description.message,
    reference: description.reference,
    fieldErrors: description.fieldErrors,
  }
}

export async function loginAction(
  _previousState: LoginState,
  formData: FormData,
): Promise<LoginState> {
  const emailValue = formData.get('email')
  const passwordValue = formData.get('password')

  const email =
    typeof emailValue === 'string'
      ? emailValue.trim()
      : ''

  const password =
    typeof passwordValue === 'string'
      ? passwordValue
      : ''

  const fieldErrors: Record<string, string[]> = {}

  if (!email) {
    fieldErrors.email = ['Email address is required.']
  }

  if (!password) {
    fieldErrors.password = ['Password is required.']
  }

  if (Object.keys(fieldErrors).length > 0) {
    return {
      message: 'Please check your information.',
      reference: null,
      fieldErrors,
    }
  }

  try {
    const baseUrl = getApiBaseUrl()

    const loginResponse = await fetch(
      `${baseUrl}/api/v1/auth/login`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          email,
          password,
        }),
        cache: 'no-store',
      },
    )

    if (!loginResponse.ok) {
      throw await errorFromResponse(loginResponse)
    }

    const loginData: unknown = await loginResponse.json()

    if (!isLoginResponse(loginData)) {
      throw new Error('Invalid login response from API.')
    }

    const refreshToken = extractRefreshToken(
      loginResponse.headers.getSetCookie(),
    )

    if (!refreshToken) {
      throw new Error(
        'The API did not return a refresh token.',
      )
    }

    const meResponse = await fetch(
      `${baseUrl}/api/v1/me`,
      {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${loginData.accessToken}`,
          Accept: 'application/json',
        },
        cache: 'no-store',
      },
    )

    if (!meResponse.ok) {
      throw await errorFromResponse(meResponse)
    }

    const meData: unknown = await meResponse.json()

    if (!isMeResponse(meData)) {
      throw new Error('Invalid user profile response.')
    }

    await createSession({
      accessToken: loginData.accessToken,
      refreshToken,
      accessTokenExpiresAtUtc: loginData.expiresAtUtc,
      userId: meData.id,
      email: meData.email,
      fullName: meData.fullName,
    })
  } catch (error) {
    console.error('RondiTrack sign-in failed', {
      status: error instanceof ApiError ? error.status : null,
      reference:
        error instanceof ApiError
          ? error.problem?.correlationId
          : null,
    })

    return loginFailure(error)
  }

  redirect('/dashboard/member')
}

export async function logoutAction(): Promise<void> {
  const session = await getSession()

  if (session) {
    try {
      const response = await fetch(
        `${getApiBaseUrl()}/api/v1/auth/logout`,
        {
          method: 'POST',
          headers: {
            Cookie: `refresh_token=${encodeURIComponent(
              session.refreshToken,
            )}`,
          },
          cache: 'no-store',
        },
      )

      if (!response.ok) {
        console.error(
          'RondiTrack logout revocation failed',
          response.status,
        )
      }
    } catch {
      console.error(
        'RondiTrack logout could not contact the API.',
      )
    }
  }

  await deleteSession()

  redirect('/login')
}
