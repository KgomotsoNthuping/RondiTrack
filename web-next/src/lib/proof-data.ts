import 'server-only'

import { notFound, redirect } from 'next/navigation'
import { z } from 'zod'

import { ApiError } from '@/api/ApiError'
import { getApiBaseUrl, signedInApi } from '@/lib/api'
import { proofPath } from '@/lib/proofFiles'

const contributionSchema = z.object({
  id: z.string().uuid(),
  stokvelId: z.string().uuid(),
  userId: z.string().uuid(),
  contributionCycleId: z.string().uuid(),
  amount: z.number(),
})

const proofMetadataSchema = z.object({
  id: z.string().uuid(),
  stokvelId: z.string().uuid(),
  contributionId: z.string().uuid(),
  uploadedByUserId: z.string().uuid(),
  contentType: z.enum([
    'application/pdf',
    'image/jpeg',
    'image/png',
  ]),
  sizeBytes: z.number().int().positive(),
  uploadedAtUtc: z.string(),
})

export type ProofContribution = z.infer<
  typeof contributionSchema
>

export type ProofMetadata = z.infer<
  typeof proofMetadataSchema
>

async function authenticatedGet(
  path: string,
): Promise<Response | null> {
  const { session } = await signedInApi()

  try {
    return await fetch(
      `${getApiBaseUrl()}${path}`,
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
    return null
  }
}

export async function getProofContribution(
  stokvelId: string,
  contributionId: string,
): Promise<ProofContribution | ApiError> {
  const response = await authenticatedGet(
    `/api/v1/stokvels/${stokvelId}/contributions/${contributionId}`,
  )

  if (!response) {
    return new ApiError(503)
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (
    response.status === 403 ||
    response.status === 404
  ) {
    notFound()
  }

  if (!response.ok) {
    return ApiError.fromResponse(response)
  }

  const payload: unknown = await response.json()
  const parsed = contributionSchema.safeParse(payload)

  if (!parsed.success) {
    return new ApiError(502)
  }

  return parsed.data
}

export async function getProofMetadata(
  stokvelId: string,
  contributionId: string,
): Promise<ProofMetadata | null | ApiError> {
  const response = await authenticatedGet(
    `${proofPath(
      stokvelId,
      contributionId,
    )}/metadata`,
  )

  if (!response) {
    return new ApiError(503)
  }

  if (response.status === 401) {
    redirect('/login?expired=1')
  }

  if (response.status === 403) {
    notFound()
  }

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    return ApiError.fromResponse(response)
  }

  const payload: unknown = await response.json()
  const parsed = proofMetadataSchema.safeParse(payload)

  if (!parsed.success) {
    return new ApiError(502)
  }

  return parsed.data
}
