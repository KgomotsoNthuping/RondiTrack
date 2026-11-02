export type HistoryKeyFilters = {
  member: string | null
  from: string | null
  to: string | null
}

export function historyFeedKey(
  stokvelId: string,
  filters: HistoryKeyFilters,
  after: string,
  firstItemId: string,
) {
  return [
    'history',
    stokvelId,
    filters.member,
    filters.from,
    filters.to,
    after,
    firstItemId,
  ] as const
}

export function activityFeedKey(
  after: string,
  firstItemId: string,
) {
  return [
    'activity',
    after,
    firstItemId,
  ] as const
}
