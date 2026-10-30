export type ContributionHistoryItem = {
  id: string
  contributionCycleId: string
  userId: string
  memberName: string
  amountInCents: number
  status: string
  paidAtUtc: string | null
  cycleNumber: number
}

export type ContributionHistoryPage = {
  items: ContributionHistoryItem[]
  nextPageToken: string
}
