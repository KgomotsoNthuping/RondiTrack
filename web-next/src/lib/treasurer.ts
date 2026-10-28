import type { MeHomeEntry } from '../api/types'

export function stokvelsWhereTreasurer(
  stokvels: readonly MeHomeEntry[],
): MeHomeEntry[] {
  return stokvels.filter(
    stokvel => stokvel.role === 'Treasurer',
  )
}
