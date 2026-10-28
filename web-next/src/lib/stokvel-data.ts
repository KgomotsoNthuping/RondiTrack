import 'server-only'
import { shouldHideStokvel } from '@/lib/stokvelVisibility'
import { notFound } from 'next/navigation'
import { ApiError } from '@/api/ApiError'
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

  if (shouldHideStokvel(result.response.status)) 
    {notFound()}

  return readOrFail(result)
}

export async function loadMemberHome() {
  try {
    return await getMemberHome()
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}

export async function loadStokvelDetail(id: string) {
  try {
    return await getStokvelDetail(id)
  } catch (error) {
    if (error instanceof ApiError) {
      return error
    }

    throw error
  }
}
