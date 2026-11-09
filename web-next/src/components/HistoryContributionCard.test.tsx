import {
  render,
  screen,
} from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { HistoryContributionCard } from './HistoryContributionCard'
import type {
  ContributionHistoryItem,
} from '@/lib/historyTypes'

const stokvelId =
  '123e4567-e89b-42d3-a456-426614174001'

const siphoId =
  '123e4567-e89b-42d3-a456-426614174002'

const thandiId =
  '123e4567-e89b-42d3-a456-426614174003'

const contribution: ContributionHistoryItem = {
  id: '123e4567-e89b-42d3-a456-426614174004',
  contributionCycleId:
    '123e4567-e89b-42d3-a456-426614174005',
  userId: thandiId,
  memberName: 'Thandi Dlamini',
  amountInCents: 15050,
  status: 'Paid',
  paidAtUtc: '2026-11-01T10:00:00Z',
  cycleNumber: 1,
}

describe('HistoryContributionCard permissions', () => {
  it('lets a Treasurer inspect another members proof', () => {
    render(
      <HistoryContributionCard
        item={contribution}
        stokvelId={stokvelId}
        viewerUserId={siphoId}
        viewerRole="Treasurer"
      />,
    )

    expect(
      screen.getByRole('link', {
        name: 'Proof of payment',
      }),
    ).toHaveAttribute(
      'href',
      `/stokvels/${stokvelId}/contributions/${contribution.id}/proof`,
    )
  })

  it('does not expose another members proof to an ordinary Member', () => {
    render(
      <HistoryContributionCard
        item={contribution}
        stokvelId={stokvelId}
        viewerUserId={siphoId}
        viewerRole="Member"
      />,
    )

    expect(
      screen.queryByRole('link', {
        name: 'Proof of payment',
      }),
    ).not.toBeInTheDocument()
  })

  it('lets a Member access their own proof', () => {
    render(
      <HistoryContributionCard
        item={contribution}
        stokvelId={stokvelId}
        viewerUserId={thandiId}
        viewerRole="Member"
      />,
    )

    expect(
      screen.getByRole('link', {
        name: 'Proof of payment',
      }),
    ).toBeInTheDocument()
  })

  it('does not treat the Admin role as Treasurer', () => {
    render(
      <HistoryContributionCard
        item={contribution}
        stokvelId={stokvelId}
        viewerUserId={siphoId}
        viewerRole="Admin"
      />,
    )

    expect(
      screen.queryByRole('link', {
        name: 'Proof of payment',
      }),
    ).not.toBeInTheDocument()
  })
})
