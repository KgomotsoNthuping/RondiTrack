
import type { components } from './schema'

// Types come directly from the generated API contract.
type Schemas = components['schemas']

// The full response from GET /api/v1/me/home.
export type MeHomeResponse =
  Schemas['MemberHomeResponse']

// One stokvel belonging to the signed-in user.
export type MeHomeEntry =
  MeHomeResponse['stokvels'][number]

// One contribution in the contributions list.
export type ContributionItem =
  Schemas['ContributionListItemResponse']
