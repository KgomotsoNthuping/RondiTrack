
import { describe, expect, it } from 'vitest'
import { renderToStaticMarkup } from 'react-dom/server'
import type { MeHomeEntry } from '../api/types'
import { StokvelCard } from './StokvelCard'

const sample: MeHomeEntry = {
  stokvelId: '11111111-1111-1111-1111-111111111111',
  name: 'Family Savers',
  role: 'Treasurer',
  currentCycle: 1,
  cycleStatus: 'Open',
  cycleDueDate: '2026-10-31',
  paidThisCycleInCents: 50000,
  stillOwedInCents: 0,
  nextPayoutDateUtc: '2026-10-31T12:00:00Z',
}

describe('StokvelCard', () => {
  it('shows Treasurer actions only for Treasurer', () => {
    const treasurer = renderToStaticMarkup(
      <StokvelCard stokvel={sample} />,
    )

    const member = renderToStaticMarkup(
      <StokvelCard
        stokvel={{ ...sample, role: 'Member' }}
      />,
    )

    expect(treasurer).toContain(
    'You&#x27;re the Treasurer',
    )
    expect(treasurer).toContain(
      'Treasurer&#x27;s next jobs',
    )

    expect(member).toContain(
      'You&#x27;re a Member',
    )
    expect(member).not.toContain(
      'Treasurer&#x27;s next jobs',
    )
  })

  it('shows the all-paid-up message', () => {
    const html = renderToStaticMarkup(
      <StokvelCard stokvel={sample} />,
    )

    expect(html).toContain(
      'All paid up for this cycle',
    )
  })

  it('shows no open cycle without money figures', () => {
    const html = renderToStaticMarkup(
      <StokvelCard
        stokvel={{
          ...sample,
          currentCycle: null,
          cycleStatus: null,
          cycleDueDate: null,
          paidThisCycleInCents: null,
          stillOwedInCents: null,
        }}
      />,
    )

    expect(html).toContain('No open cycle')
    expect(html).not.toContain('Paid this cycle:')
    expect(html).not.toContain('Still owed:')
  })

  it('handles a missing payout date', () => {
    const html = renderToStaticMarkup(
      <StokvelCard
        stokvel={{
          ...sample,
          nextPayoutDateUtc: null,
        }}
      />,
    )

    expect(html).toContain('No payout scheduled')
  })
})
