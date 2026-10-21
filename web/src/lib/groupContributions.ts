import type { components } from '@/api/schema'

export type Cycle = components['schemas']['ContributionCycleResponse']
export type Contribution = components['schemas']['ContributionListItemResponse']

export type ContributionGroup = {
  id: string
  cycle: Cycle | null
  contributions: Contribution[]
}

function newestFirst(a: Contribution, b: Contribution): number {
  const aTime = a.paidAtUtc ? Date.parse(a.paidAtUtc) : 0
  const bTime = b.paidAtUtc ? Date.parse(b.paidAtUtc) : 0
  return bTime - aTime || b.cycleNumber - a.cycleNumber || a.id.localeCompare(b.id)
}

// Pure: no React state, clocks or API calls. Never discard an unknown cycle.
export function groupContributionsByCycle(
  cycles: readonly Cycle[],
  contributions: readonly Contribution[],
): ContributionGroup[] {
  const groups: ContributionGroup[] = [...cycles]
    .sort((a, b) => b.cycleNumber - a.cycleNumber)
    .map((cycle) => ({ id: cycle.id, cycle, contributions: [] as Contribution[] }))

  const byId = new Map(groups.map((group) => [group.id, group]))
  const unknown: Contribution[] = []
  for (const contribution of contributions) {
    const group = byId.get(contribution.contributionCycleId)
    if (group) group.contributions.push(contribution)
    else unknown.push(contribution)
  }
  for (const group of groups) group.contributions.sort(newestFirst)
  if (unknown.length > 0) {
    groups.push({ id: 'unknown-cycle', cycle: null, contributions: unknown.sort(newestFirst) })
  }
  return groups
}
