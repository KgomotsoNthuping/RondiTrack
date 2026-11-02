export type MemberActivityItem = {
  id: string
  type: 'Contribution' | 'Payout' | 'MemberJoined'
  stokvelId: string
  stokvelName: string
  memberId: string
  memberName: string
  occurredAtUtc: string
  amountInCents: number | null
}

export type MemberActivityPage = {
  items: MemberActivityItem[]
  nextPageToken: string
}

export const ACTIVITY_PAGE_SIZE = 5
