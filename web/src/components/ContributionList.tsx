import { useState } from 'react'
import { ChevronDown, ChevronsDownUp, ChevronsUpDown } from 'lucide-react'
import { ContributionCard } from './ContributionCard'
import { StatusBadge } from './StatusBadge'
import { groupContributionsByCycle, type Contribution, type Cycle } from '@/lib/groupContributions'
import { formatCents } from '@/lib/money'

type Props = {
  cycles: readonly Cycle[]
  contributions: readonly Contribution[]
}

function saDate(date: string | null | undefined): string {
  if (!date) return 'Not scheduled'
  // DateOnly: add midday UTC to avoid a browser-local timezone rollover.
  return new Intl.DateTimeFormat('en-ZA', {
    timeZone: 'Africa/Johannesburg', day: '2-digit', month: 'short', year: 'numeric',
  }).format(new Date(`${date}T12:00:00Z`))
}

export function ContributionList({ cycles, contributions }: Props) {
  const groups = groupContributionsByCycle(cycles, contributions)
  const [overrides, setOverrides] = useState<Record<string, boolean>>({})
  const isOpen = (group: (typeof groups)[number]) =>
    overrides[group.id] ?? group.cycle?.status === 'Open'
  const allOpen = groups.length > 0 && groups.every(isOpen)

  return (
    <section aria-label="Contributions grouped by cycle" className="min-w-0 space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-bold tracking-tight">Contributions by cycle</h2>
        <button type="button" onClick={() => setOverrides(Object.fromEntries(groups.map(g => [g.id, !allOpen])))}
          className="inline-flex min-h-11 items-center gap-2 rounded-lg border border-border bg-card px-4 text-sm font-semibold text-card-foreground hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring">
          {allOpen ? <ChevronsDownUp className="size-4" aria-hidden /> : <ChevronsUpDown className="size-4" aria-hidden />}
          {allOpen ? 'Collapse all' : 'Expand all'}
        </button>
      </div>
      {groups.length === 0 && <p className="text-muted-foreground">No cycles or contributions available.</p>}
      <div className="space-y-3">
        {groups.map(group => {
          const expanded = isOpen(group)
          const headingId = `cycle-heading-${group.id}`
          const bodyId = `cycle-body-${group.id}`
          return (
            <article key={group.id} className="min-w-0 overflow-hidden rounded-xl border border-border bg-card text-card-foreground shadow-sm">
              <h3 id={headingId}>
                <button type="button" aria-expanded={expanded} aria-controls={bodyId}
                  onClick={() => setOverrides(prev => ({ ...prev, [group.id]: !expanded }))}
                  className="flex min-h-14 w-full min-w-0 items-start justify-between gap-3 px-4 py-4 text-left hover:bg-accent/40 focus-visible:ring-2 focus-visible:ring-ring">
                  <span className="min-w-0 flex-1 space-y-2">
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="font-bold break-words">{group.cycle ? `Cycle ${group.cycle.cycleNumber}` : 'Unknown cycle'}</span>
                      {group.cycle && <StatusBadge kind="cycle" status={group.cycle.status} />}
                    </span>
                    {group.cycle && <span className="block space-y-1 text-sm text-muted-foreground">
                      <span className="block">Due date: {saDate(group.cycle.dueDate)}</span>
                      <span className="block">Collected: <span className="whitespace-nowrap font-semibold text-foreground">{formatCents(group.cycle.totalCollectedInCents)}</span>
                        {' / '} Due: <span className="whitespace-nowrap font-semibold text-foreground">{formatCents(group.cycle.amountDueInCents)}</span></span>
                    </span>}
                  </span>
                  <ChevronDown aria-hidden className={`mt-1 size-5 shrink-0 transition-transform ${expanded ? 'rotate-180' : ''}`} />
                </button>
              </h3>
              {expanded && <div id={bodyId} aria-labelledby={headingId} className="space-y-3 border-t border-border p-4">
                {group.contributions.length === 0
                  ? <p className="text-sm text-muted-foreground">No contributions yet</p>
                  : group.contributions.map(item => <div key={item.id} className="min-w-0"><ContributionCard contribution={item} /></div>)}
              </div>}
            </article>
          )
        })}
      </div>
    </section>
  )
}
