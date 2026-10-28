import Link from 'next/link'
import {
  CalendarDays,
  CircleDollarSign,
  Wallet,
} from 'lucide-react'
import type { MeHomeEntry } from '@/api/types'
import { formatCents } from '@/lib/money'
import {
  formatCalendarDate,
  formatDate,
} from '@/lib/dates'
import { roleLabel } from '@/lib/roles'
import {
  ArrearsBadge,
  StatusBadge,
} from './StatusBadge'

type Props = {
  stokvel: MeHomeEntry
}

export function StokvelCard({ stokvel }: Props) {
  const hasOpenCycle =
    stokvel.cycleStatus === 'Open' &&
    stokvel.currentCycle != null

  return (
    <article className="flex h-full flex-col rounded-2xl border border-slate-200 bg-white p-5 text-slate-900 shadow-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white">
      <header className="flex items-start gap-3 border-b border-slate-200 pb-4 dark:border-slate-700">
        <div className="rounded-xl bg-lime-100 p-3 text-lime-900">
          <Wallet aria-hidden="true" className="size-5" />
        </div>
        <div className="min-w-0 flex-1">
          <h2 className="break-words text-lg font-bold">
            {stokvel.name}
          </h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
            {roleLabel(stokvel.role)}
          </p>
        </div>
      </header>

      <div className="flex-1 space-y-4 py-5">
        {stokvel.currentCycle != null ? (
          <section className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h3 className="font-semibold">
                Cycle {stokvel.currentCycle}
              </h3>
              <StatusBadge
                kind="cycle"
                status={stokvel.cycleStatus}
              />
            </div>

            <p className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
              <CalendarDays
                aria-hidden="true"
                className="size-4"
              />
              Due date:{' '}
              {stokvel.cycleDueDate
                ? formatCalendarDate(stokvel.cycleDueDate)
                : 'Not scheduled'}
            </p>

            {hasOpenCycle && (
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-xl bg-lime-50 p-3 text-lime-950">
                  <p className="text-xs">Paid</p>
                  <p className="mt-1 break-words font-bold">
                    {stokvel.paidThisCycleInCents == null
                      ? 'Unavailable'
                      : formatCents(
                          stokvel.paidThisCycleInCents,
                        )}
                  </p>
                </div>

                <div className="rounded-xl bg-slate-100 p-3 text-slate-950">
                  <p className="text-xs">Still owed</p>
                  <p className="mt-1 break-words font-bold">
                    {stokvel.stillOwedInCents == null
                      ? 'Unavailable'
                      : formatCents(
                          stokvel.stillOwedInCents,
                        )}
                  </p>
                </div>
              </div>
            )}
          </section>
        ) : (
          <p className="text-sm text-slate-600 dark:text-slate-300">
            No contribution cycle available.
          </p>
        )}

        <ArrearsBadge
          amountInCents={stokvel.arrearsInCents}
        />
      </div>

      <div className="space-y-3 border-t border-slate-200 pt-4 dark:border-slate-700">
        <div className="flex items-center gap-2 text-sm">
          <CircleDollarSign
            aria-hidden="true"
            className="size-4 text-lime-700"
          />
          <span>
            Next payout:{' '}
            {stokvel.nextPayoutDateUtc
              ? formatDate(stokvel.nextPayoutDateUtc)
              : 'Not scheduled'}
          </span>
        </div>

        <Link
          href={`/stokvels/${stokvel.stokvelId}`}
          className="inline-flex min-h-11 w-full items-center justify-center rounded-lg bg-lime-400 px-4 py-2 font-semibold text-lime-950 hover:bg-lime-300"
        >
          View stokvel
        </Link>
      </div>
    </article>
  )
}