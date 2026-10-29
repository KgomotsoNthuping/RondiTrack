'use server'

import { randomUUID } from 'node:crypto'
import { refresh } from 'next/cache'
import { redirect } from 'next/navigation'
import { z } from 'zod'

import { ApiError } from '@/api/ApiError'
import { asProblem, signedInApi } from '@/lib/api'
import { describeContributionFailure } from '@/lib/contributionErrors'
import { formatCents } from '@/lib/money'
import {
  contributionSchema,
  toContributionRequest,
} from '@/forms/contribution'

export type RecordContributionState = {
  status: 'idle' | 'success' | 'error' | 'uncertain'
  attemptKey: string | null
  nextKey: string | null
  message: string | null
  reference: string | null
  fieldErrors: Record<string, string[]>
}

const recordSchema = contributionSchema.extend({
  stokvelId: z.string().uuid(),
  cycleId: z.string().uuid(),
  idempotencyKey: z.string().uuid(),
})

export async function recordContributionAction(
  _previousState: RecordContributionState,
  formData: FormData,
): Promise<RecordContributionState> {
  // Check authentication before validating or writing.
  const { api } = await signedInApi()

  const rawKey = formData.get('idempotencyKey')

  const attemptKey =
    typeof rawKey === 'string' ? rawKey : null

  const parsed = recordSchema.safeParse({
    stokvelId: formData.get('stokvelId'),
    cycleId: formData.get('cycleId'),
    userId: formData.get('userId'),
    amount: formData.get('amount'),
    idempotencyKey: rawKey,
  })

  if (!parsed.success) {
    const fieldErrors: Record<string, string[]> = {}

    for (const issue of parsed.error.issues) {
      const field = String(issue.path[0] ?? 'form')

      fieldErrors[field] ??= []
      fieldErrors[field].push(issue.message)
    }

    return {
      status: 'error',
      attemptKey,
      nextKey: null,
      message:
        'Please correct the highlighted information.',
      reference: null,
      fieldErrors,
    }
  }

  const values = parsed.data
  const contribution = toContributionRequest(values)

  /*
   * One POST with the submitted Idempotency-Key.
   *
   * No automatic retry here: a second POST after
   * an uncertain outcome must be explicitly retried
   * using the SAME key and SAME payment details.
   */
  const result = await api.POST(
    '/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions',
    {
      params: {
        path: {
          stokvelId: values.stokvelId,
          cycleId: values.cycleId,
        },
        header: {
          'Idempotency-Key': values.idempotencyKey,
        },
      },
      body: {
        userId: contribution.userId,
        amountInCents: contribution.amountInCents,
      },
    },
  ).catch(() => null)

  if (result === null) {
    console.error(
      'Contribution outcome could not be confirmed.',
    )

    return {
      status: 'uncertain',
      attemptKey: values.idempotencyKey,
      nextKey: null,
      message:
        'The connection was interrupted. Your contribution may have been recorded. Retry with the same details to check safely.',
      reference: null,
      fieldErrors: {},
    }
  }

  if (result.response.status === 401) {
    redirect('/login?expired=1')
  }

  /*
   * The actual backend returns 201 Created.
   * response.ok accepts both 200 and 201.
   */
  if (result.response.ok) {
    // We deliberately do not cache authenticated
    // balances, so refresh the active server view.
    refresh()

    return {
      status: 'success',
      attemptKey: values.idempotencyKey,
      nextKey: randomUUID(),
      message:
        `${formatCents(contribution.amountInCents)} recorded successfully.`,
      reference: null,
      fieldErrors: {},
    }
  }

  const error = new ApiError(
    result.response.status,
    asProblem(result.error),
    result.response.headers.get('Retry-After'),
  )

  const failure = describeContributionFailure(error)

  return {
    ...failure,
    attemptKey: values.idempotencyKey,
    nextKey: null,
  }
}
