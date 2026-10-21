import type { Cycle } from '@/lib/groupContributions'
import { home } from './sample'

// Saved DEMO response shape, not a live fetch. Replace with captured /cycles JSON
// before demonstrating financial accuracy. Totals are supplied, never calculated in React.
const familyId = home.find((s) => s.name === 'Family Savers')?.stokvelId ?? ''

export const cycles: Cycle[] = [
  {
    id: 'cycle-001', stokvelId: familyId, cycleNumber: 1,
    targetAmount: 1000, version: 0, status: 'Open', dueDate: '2026-10-31',
    totalCollectedInCents: 15050, amountDueInCents: 100000,
  },
  {
    id: 'cycle-002', stokvelId: familyId, cycleNumber: 2,
    targetAmount: 1000, version: 0, status: 'Closed', dueDate: '2026-09-30',
    totalCollectedInCents: 75000, amountDueInCents: 100000,
  },
  {
    id: 'cycle-003', stokvelId: familyId, cycleNumber: 3,
    targetAmount: 1000, version: 0, status: 'PaidOut', dueDate: '2026-08-31',
    totalCollectedInCents: 100000, amountDueInCents: 100000,
  },
]
