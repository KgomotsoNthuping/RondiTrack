import type { components } from '@/api/schema'

type ApiMember = components['schemas']['UserResponse']

export type RecordableMember = {
  id: string
  fullName: string
}

export function canSelectContributionMember(
  role: string | null | undefined,
): boolean {
  return role === 'Treasurer'
}

export function toRecordableMembers(
  members: readonly ApiMember[],
): RecordableMember[] {
  return members.map(member => ({
    id: member.id,
    fullName: member.fullName,
  }))
}
