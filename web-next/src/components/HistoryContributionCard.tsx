import { StatusBadge } from '@/components/StatusBadge'
import { formatCents } from '@/lib/money'
import { formatDateTime } from '@/lib/dates'
import type { ContributionHistoryItem } from '@/lib/historyTypes'

export function HistoryContributionCard({
  item,
}: {
  item: ContributionHistoryItem
}) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-sm text-slate-600">
            {item.memberName}
          </p>

          <h3 className="mt-2 text-2xl font-bold">
            {formatCents(item.amountInCents)}
          </h3>
        </div>

        <StatusBadge
          kind="contribution"
          status={item.status}
        />
      </div>

      <div className="mt-5 grid gap-3 border-t border-slate-200 pt-4 text-sm sm:grid-cols-2">
        <div>
          <p className="text-slate-500">
            Contribution cycle
          </p>

          <p className="mt-1 font-semibold">
            Cycle {item.cycleNumber}
          </p>
        </div>

        <div>
          <p className="text-slate-500">
            Payment date
          </p>

          <p className="mt-1 font-semibold">
            {item.paidAtUtc
              ? formatDateTime(item.paidAtUtc)
              : 'Not paid yet'}
          </p>
        </div>
      </div>
    </article>
  )
}
