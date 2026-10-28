import {
  CalendarClock,
  CreditCard,
  Layers3,
} from 'lucide-react'

import type { ContributionItem } from '../api/types'
import { formatCents } from '../lib/money'
import { formatDateTime } from '../lib/dates'
import { StatusBadge } from './StatusBadge'

type ContributionCardProps = {
  contribution: ContributionItem
}

export function ContributionCard({
  contribution,
}: ContributionCardProps) {
  const isPending = contribution.status === 'Pending'

  const paymentDate =
    !isPending && contribution.paidAtUtc != null
      ? formatDateTime(contribution.paidAtUtc)
      : null

  return (
    <article className="min-w-0 rounded-xl border border-slate-200 bg-white p-4 text-slate-900 shadow-sm transition-colors hover:border-lime-400 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:hover:border-lime-400 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-lime-100 text-lime-900 dark:bg-lime-300 dark:text-lime-950">
            <CreditCard aria-hidden="true" className="size-5" />
          </div>

          <div className="min-w-0">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-300">
              Contribution
            </p>

            <h3 className="mt-1 break-words text-xl font-bold tracking-tight text-slate-950 dark:text-white">
              {formatCents(contribution.amountInCents)}
            </h3>
          </div>
        </div>

        <StatusBadge
          kind="contribution"
          status={contribution.status}
        />
      </div>

      <div className="mt-4 grid gap-3 border-t border-slate-200 pt-4 dark:border-slate-700 sm:grid-cols-2">
        <div className="flex items-start gap-2">
          <Layers3
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0 text-lime-700 dark:text-lime-300"
          />

          <div>
            <p className="text-xs text-slate-500 dark:text-slate-300">
              Contribution cycle
            </p>
            <p className="mt-1 text-sm font-semibold text-slate-900 dark:text-white">
              Cycle {contribution.cycleNumber}
            </p>
          </div>
        </div>

        <div className="flex items-start gap-2">
          <CalendarClock
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0 text-lime-700 dark:text-lime-300"
          />

          <div className="min-w-0">
            <p className="text-xs text-slate-500 dark:text-slate-300">
              Payment date
            </p>

            <p className="mt-1 break-words text-sm font-semibold text-slate-900 dark:text-white">
              {paymentDate ?? 'Not paid yet'}
            </p>
          </div>
        </div>
      </div>
    </article>
  )
}
