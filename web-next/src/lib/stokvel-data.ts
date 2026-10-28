import 'server-only'

import { notFound } from 'next/navigation'
import type {
  MeHomeResponse,
  StokvelDetail,
} from '@/api/types'
import { readOrFail, signedInApi } from '@/lib/api'

export async function getMemberHome(): Promise<MeHomeResponse> {
  const { api } = await signedInApi()

  const result = await api.GET('/api/v1/me/home')

  return readOrFail(result)
}

export async function getStokvelDetail(
  id: string,
): Promise<StokvelDetail> {
  const { api } = await signedInApi()

  const result = await api.GET('/api/v1/stokvels/{id}', {
    params: {
      path: { id },
    },
  })

  // The backend may return 403 for non-members.
  // Treat this the same as a nonexistent stokvel.
  if (
    result.response.status === 403 ||
    result.response.status === 404
  ) {
    notFound()
  }

  return readOrFail(result)
}
