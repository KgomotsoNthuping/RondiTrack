
'use server'

import { randomUUID } from 'node:crypto'
import { refresh } from 'next/cache'
import { redirect } from 'next/navigation'

import { ApiError } from '@/api/ApiError'
import { getApiBaseUrl } from '@/lib/api'
import {
  isUuid,
  proofPath,
  validateProofFile,
} from '@/lib/proofFiles'
import { getSession } from '@/lib/session'

export type ProofActionState = {
  status:
    | 'idle'
    | 'success'
    | 'invalid'
    | 'conflict'
    | 'missing'
    | 'unexpected'
  resultId: string
  message: string | null
  fileError: string | null
  reference: string | null
}

export const initialProofState: ProofActionState = {
  status: 'idle',
  resultId: '',
  message: null,
  fileError: null,
  reference: null,
}

function result(
  status: ProofActionState['status'],
  message: string,
  fileError: string | null = null,
  reference: string | null = null,
): ProofActionState {
  return {
    status,
    resultId: randomUUID(),
    message,
    fileError,
    reference,
  }
}

function idsFromForm(
  formData: FormData,
): {
  stokvelId: string
  contributionId: string
} | null {
  const stokvelId = formData.get('stokvelId')
  const contributionId = formData.get('contributionId')

  if (
    typeof stokvelId !== 'string' ||
    typeof contributionId !== 'string' ||
    !isUuid(stokvelId) ||
    !isUuid(contributionId)
  ) {
    return null
  }

  return { stokvelId, contributionId }
}

async function failureFromResponse(
  response: Response,
): Promise<ProofActionState> {
  const error = await ApiError.fromResponse(response)
  const reference = error.problem?.correlationId ?? null

  if (response.status === 400 || response.status === 413) {
    return result(
      'invalid',
      'Please check the selected file.',
      error.problem?.errors?.file?.[0] ??
        'Choose a valid PDF, JPEG or PNG below 5 MiB.',
    )
  }

  if (response.status === 409) {
    return result(
      'conflict',
      'This contribution cycle is closed. Proof of payment can no longer be changed.',
    )
  }

  if (response.status === 403 || response.status === 404) {
    return result(
      'missing',
      'This contribution is unavailable or you do not have permission to change its proof.',
    )
  }

  return result(
    'unexpected',
    'The proof service could not complete the request.',
    null,
    reference,
  )
}

export async function uploadProofAction(
  _previous: ProofActionState,
  formData: FormData,
): Promise<ProofActionState> {
  const ids = idsFromForm(formData)

  if (!ids) {
    return result(
      'invalid',
      'The contribution information is invalid.',
    )
  }

  const selectedFile = formData.get('file')

  if (!(selectedFile instanceof File)) {
    return result(
      'invalid',
      'Please choose a file.',
      'Choose a PDF, JPEG or PNG file.',
    )
  }

  const fileError = validateProofFile(selectedFile)

  if (fileError) {
    return result(
      'invalid',
      'Please check the selected file.',
      fileError,
    )
  }

  const session = await getSession()

  if (!session) {
    redirect('/login?expired=1')
  }

  const payload = new FormData()
  payload.set('file', selectedFile, selectedFile.name)

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}${proofPath(
        ids.stokvelId,
        ids.contributionId,
      )}`,
      {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          Accept: 'application/json',
        },
        body: payload,
        cache: 'no-store',
      },
    )
  } catch {
    return result(
      'unexpected',
      'The API could not be reached. The upload result is uncertain; check whether a proof exists before trying again.',
    )
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (!response.ok) {
    return failureFromResponse(response)
  }

  refresh()

  return result(
    'success',
    'Proof of payment uploaded successfully.',
  )
}

export async function deleteProofAction(
  _previous: ProofActionState,
  formData: FormData,
): Promise<ProofActionState> {
  const ids = idsFromForm(formData)

  if (!ids) {
    return result(
      'invalid',
      'The contribution information is invalid.',
    )
  }

  const session = await getSession()

  if (!session) {
    redirect('/login?expired=1')
  }

  let response: Response

  try {
    response = await fetch(
      `${getApiBaseUrl()}${proofPath(
        ids.stokvelId,
        ids.contributionId,
      )}`,
      {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
        },
        cache: 'no-store',
      },
    )
  } catch {
    return result(
      'unexpected',
      'The API could not be reached. Check whether the proof still exists before retrying.',
    )
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (!response.ok) {
    return failureFromResponse(response)
  }

  refresh()

  return result(
    'success',
    'Proof of payment removed successfully.',
  )
}
