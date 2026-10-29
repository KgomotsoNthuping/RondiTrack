'use server'

import { redirect } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
import { registrationSchema } from '@/forms/registration'
import { getApiBaseUrl } from '@/lib/api'
import { describeError } from '@/lib/errorMessages'
import { safeNext } from '@/lib/safeNext'

export type RegistrationState = {
  message: string | null
  reference: string | null
  fieldErrors: Record<string, string[]>
}

function mapServerFieldErrors(
  errors: Record<string, string[]> | undefined,
): Record<string, string[]> {
  const mapped: Record<string, string[]> = {}

  const fields = [
    'fullName',
    'email',
    'phoneNumber',
    'password',
    'confirmPassword',
  ] as const

  for (const [key, messages] of Object.entries(
    errors ?? {},
  )) {
    if (!Array.isArray(messages)) {
      continue
    }

    const normalized = key
      .toLowerCase()
      .replace(/[^a-z]/g, '')

    const field = fields.find(name =>
      normalized.endsWith(name.toLowerCase()),
    )

    if (field) {
      mapped[field] = messages.filter(
        (message): message is string =>
          typeof message === 'string',
      )
    }
  }

  return mapped
}

export async function registerAction(
  _previousState: RegistrationState,
  formData: FormData,
): Promise<RegistrationState> {
  const parsed = registrationSchema.safeParse({
    fullName: formData.get('fullName'),
    email: formData.get('email'),
    phoneNumber: formData.get('phoneNumber'),
    password: formData.get('password'),
    confirmPassword: formData.get('confirmPassword'),
  })

  if (!parsed.success) {
    const fieldErrors: Record<string, string[]> = {}

    for (const issue of parsed.error.issues) {
      const field = String(issue.path[0] ?? 'form')

      fieldErrors[field] ??= []
      fieldErrors[field].push(issue.message)
    }

    return {
      message: 'Please correct your registration details.',
      reference: null,
      fieldErrors,
    }
  }

  const { fullName, email, phoneNumber, password } =
    parsed.data

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}/api/v1/auth/register`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          fullName,
          email,
          phoneNumber,
          password,
        }),
        cache: 'no-store',
      },
    )
  } catch {
    return {
      message:
        'RondiTrack could not connect to the server. Please try again.',
      reference: null,
      fieldErrors: {},
    }
  }

  if (!response.ok) {
    const error = await ApiError.fromResponse(response)
    const description = describeError(error)

    /*
     * Avoid confirming whether a particular email
     * address already belongs to an account.
     */
    if (error.status === 409) {
      return {
        message:
          'We could not create an account using those details. Check your information or sign in if you already have an account.',
        reference: description.reference,
        fieldErrors: {},
      }
    }

    if (
      error.status === 400 ||
      error.status === 422
    ) {
      return {
        message:
          'Please correct your registration details.',
        reference: description.reference,
        fieldErrors: mapServerFieldErrors(
          error.problem?.errors,
        ),
      }
    }

    return {
      message: description.message,
      reference: description.reference,
      fieldErrors: {},
    }
  }

  /*
   * Registration succeeded.
   * The API returns a user, not login tokens.
   * The person must now sign in.
   */
  const destination = safeNext(formData.get('next'))

  const params = new URLSearchParams({
    registered: '1',
  })

  if (destination) {
    params.set('next', destination)
  }

  // redirect() must be outside try/catch.
  redirect(`/login?${params.toString()}`)
}
