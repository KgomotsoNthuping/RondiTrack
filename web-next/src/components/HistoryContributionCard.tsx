import Link from 'next/link'

import { StatusBadge } from '@/components/StatusBadge'
import { formatCents } from '@/lib/money'
import { formatDateTime } from '@/lib/dates'
import type { ContributionHistoryItem } from '@/lib/historyTypes'

type Props = {
  item: ContributionHistoryItem
  stokvelId: string
  viewerUserId: string
  viewerRole: string
}

export function HistoryContributionCard({
  item,
  stokvelId,
  viewerUserId,
  viewerRole,
}: Props) {
  const canAccessProof =
    item.userId === viewerUserId ||
    viewerRole === 'Treasurer'

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

      {canAccessProof && (
        <Link
          href={`/stokvels/${stokvelId}/contributions/${item.id}/proof`}
          className="mt-5 inline-flex min-h-11 items-center rounded-xl border border-lime-600 px-4 py-3 text-sm font-semibold text-lime-800 hover:bg-lime-50"
        >
          Proof of payment
        </Link>
      )}
    </article>
  )
}
