
import {
  CalendarDays,
  CheckCircle2,
  CircleDollarSign,
  ClipboardList,
  Wallet,
} from 'lucide-react'

import type { MeHomeEntry } from '../api/types'
import { formatCents } from '../lib/money'
import { formatDate, formatCalendarDate } from '../lib/dates'
import { isTreasurerRole, roleLabel } from '../lib/roles'
import { ArrearsBadge, StatusBadge } from './StatusBadge'

type StokvelCardProps = {
  stokvel: MeHomeEntry
}

export function StokvelCard({ stokvel }: StokvelCardProps) {
  const isTreasurer = isTreasurerRole(stokvel.role)

  const hasOpenCycle =
    stokvel.cycleStatus === 'Open' &&
    stokvel.currentCycle != null

  const hasPaymentDetails =
    stokvel.paidThisCycleInCents != null &&
    stokvel.stillOwedInCents != null

  const allPaidUp =
    hasOpenCycle &&
    stokvel.stillOwedInCents === 0

  return (
    <article className="flex h-full min-w-0 flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white p-5 text-slate-900 shadow-sm transition-shadow hover:shadow-md dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 sm:p-6">
      <header className="flex min-w-0 items-start gap-3 border-b border-slate-200 pb-4 dark:border-slate-700">
        <div className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-lime-100 text-lime-900 dark:bg-lime-300 dark:text-lime-950">
          <Wallet aria-hidden="true" className="size-5" />
        </div>

        <div className="min-w-0 flex-1">
          <h2 className="break-words text-lg font-bold tracking-tight text-slate-950 dark:text-white">
            {stokvel.name}
          </h2>

          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
            {roleLabel(stokvel.role)}
          </p>
        </div>
      </header>

      <div className="space-y-4 py-5">
        {stokvel.currentCycle != null ? (
          <section className="space-y-3" aria-label="Current cycle">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-base font-semibold text-slate-950 dark:text-white">
                Cycle {stokvel.currentCycle}
              </h3>

              <StatusBadge
                kind="cycle"
                status={stokvel.cycleStatus}
              />
            </div>

            <div className="flex items-start gap-2 text-sm text-slate-600 dark:text-slate-300">
              <CalendarDays
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0 text-lime-700 dark:text-lime-300"
              />

              <p>
                Due date:{' '}
                <span className="font-medium text-slate-900 dark:text-slate-100">
                  {stokvel.cycleDueDate != null
                    ? formatCalendarDate(stokvel.cycleDueDate)
                    : 'No due date set'}
                </span>
              </p>
            </div>

            {hasOpenCycle && hasPaymentDetails ? (
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="min-w-0 rounded-xl bg-lime-50 p-4 dark:bg-lime-950/40">
                  <p className="text-xs font-medium text-slate-600 dark:text-slate-300">
                    Paid this cycle
                  </p>
                  <p className="mt-2 break-words text-xl font-bold text-lime-900 dark:text-lime-200">
                    {formatCents(stokvel.paidThisCycleInCents!)}
                  </p>
                </div>

                <div className="min-w-0 rounded-xl bg-slate-50 p-4 dark:bg-slate-800">
                  <p className="text-xs font-medium text-slate-600 dark:text-slate-300">
                    Still owed
                  </p>
                  <p className="mt-2 break-words text-xl font-bold text-slate-950 dark:text-white">
                    {formatCents(stokvel.stillOwedInCents!)}
                  </p>
                </div>
              </div>
            ) : hasOpenCycle ? (
              <p className="text-sm text-slate-600 dark:text-slate-300">
                Payment details not available
              </p>
            ) : (
              <p className="text-sm text-slate-600 dark:text-slate-300">
                No open cycle
              </p>
            )}

            {allPaidUp && (
              <div className="flex items-center gap-2 rounded-xl bg-lime-100 px-3 py-2 text-sm font-semibold text-lime-950 dark:bg-lime-300 dark:text-lime-950">
                <CheckCircle2 aria-hidden="true" className="size-4 shrink-0" />
                All paid up for this cycle
              </div>
            )}
          </section>
        ) : (
          <div className="rounded-xl bg-slate-50 px-4 py-5 dark:bg-slate-800">
            <p className="text-sm font-medium text-slate-700 dark:text-slate-200">
              No open cycle
            </p>
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-300">
              Cycle information will appear here when available.
            </p>
          </div>
        )}

        <ArrearsBadge amountInCents={stokvel.arrearsInCents} />
      </div>

      <section className="mt-auto flex items-start gap-3 border-t border-slate-200 pt-4 dark:border-slate-700">
        <CircleDollarSign
          aria-hidden="true"
          className="mt-0.5 size-5 shrink-0 text-lime-700 dark:text-lime-300"
        />

        <div className="min-w-0">
          <h3 className="text-sm font-semibold text-slate-950 dark:text-white">
            Next payout
          </h3>

          <p className="mt-1 break-words text-sm text-slate-600 dark:text-slate-300">
            {stokvel.nextPayoutDateUtc != null
              ? formatDate(stokvel.nextPayoutDateUtc)
              : 'No payout scheduled'}
          </p>
        </div>
      </section>

      {isTreasurer && (
        <section className="mt-5 rounded-xl border border-lime-200 bg-lime-50 p-4 dark:border-lime-700 dark:bg-lime-950/40">
          <div className="flex items-center gap-2">
            <ClipboardList
              aria-hidden="true"
              className="size-4 text-lime-800 dark:text-lime-300"
            />
            <h3 className="text-sm font-bold text-slate-950 dark:text-white">
              Treasurer's next jobs
            </h3>
          </div>

          <ul className="mt-3 list-inside list-disc space-y-1 text-sm text-slate-700 dark:text-slate-200">
            <li>Close cycle (Week 8)</li>
            <li>Record payout (Week 8)</li>
          </ul>
        </section>
      )}
    </article>
  )
}
