import type { components } from './schema'

type Schemas = components['schemas']

export type MeHomeResponse =
  Schemas['MemberHomeResponse']

export type MeHomeEntry =
  MeHomeResponse['stokvels'][number]

export type ContributionItem =
  Schemas['ContributionListItemResponse']

export type StokvelDetail =
  Schemas['StokvelDetailResponse']

export type StokvelCycleSummary =
  Schemas['StokvelCycleSummaryResponse']