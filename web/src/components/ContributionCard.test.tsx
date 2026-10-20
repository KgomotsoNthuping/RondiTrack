import { describe, expect, it } from 'vitest'
import { renderToStaticMarkup } from 'react-dom/server'

import type { ContributionItem } from '../api/types'
import { ContributionCard } from './ContributionCard'

const payment: ContributionItem = {
  id: '11111111-1111-1111-1111-111111111111',
  amountInCents: 10,
  status: 'Paid',
  paidAtUtc: '2026-10-31T22:30:00Z',
  cycleNumber: 1,
}

describe('ContributionCard', () => {
  it('displays the amount and cycle', () => {
    const html = renderToStaticMarkup(
      <ContributionCard contribution={payment} />,
    )

    expect(html).toContain('0,10')
    expect(html).toContain('Cycle 1')
    expect(html).toContain('Paid')
  })

  it('displays the midnight payment in SA time', () => {
    const html = renderToStaticMarkup(
      <ContributionCard contribution={payment} />,
    )

    expect(html).toContain('01 Nov 2026, 00:30')
  })

  it('handles pending contributions', () => {
    const pending: ContributionItem = {
      ...payment,
      status: 'Pending',
      paidAtUtc: null,
    }

    const html = renderToStaticMarkup(
      <ContributionCard contribution={pending} />,
    )

    expect(html).toContain('Pending')
    expect(html).toContain('Not paid yet')
  })

  it('shows confirmed contributions', () => {
    const html = renderToStaticMarkup(
      <ContributionCard
        contribution={{
          ...payment,
          status: 'Confirmed',
        }}
      />,
    )

    expect(html).toContain('Confirmed')
  })
})
