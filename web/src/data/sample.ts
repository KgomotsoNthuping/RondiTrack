import type {
  MeHomeEntry,
  ContributionItem,
} from '../api/types'

export const home: MeHomeEntry[] = [
  {
    stokvelId: '1111111111111',
    name: 'Family Savers',
    role: 'Treasurer',
    currentCycle: 1,
    cycleStatus: 'Open',
    cycleDueDate: null,
    paidThisCycleInCents: null,
    stillOwedInCents: null,
    nextPayoutDateUtc: null,
    arrearsInCents: 0,
  },
  {
    stokvelId: '222-222',
    name: 'Home Savers',
    role: 'Member',
    currentCycle: null,
    cycleStatus: null,
    cycleDueDate: null,
    paidThisCycleInCents: null,
    stillOwedInCents: null,
    nextPayoutDateUtc: null,
    arrearsInCents: 0,
  }
]


export const contributions: ContributionItem[] = [
    {
        id: '3333-333',
        contributionCycleId: 'cycle-001',
        amountInCents: 15050,
        status : 'Pending',
        paidAtUtc : '2026-10-20',
        cycleNumber : 1,
    }
]
