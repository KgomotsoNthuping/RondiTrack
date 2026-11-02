import Link from 'next/link'
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  UserRoundPlus,
} from 'lucide-react'

import type { MemberActivityItem } from '@/lib/activityTypes'
import { formatCents } from '@/lib/money'
import { formatDateTime } from '@/lib/dates'

export function ActivityCard({
  item,
}: {
  item: MemberActivityItem
}) {
  const isContribution =
    item.type === 'Contribution'

  const isPayout =
    item.type === 'Payout'

  const Icon = isContribution
    ? ArrowDownToLine
    : isPayout
      ? ArrowUpFromLine
      : UserRoundPlus

  const title = isContribution
    ? 'Contribution recorded'
    : isPayout
      ? 'Payout made'
      : 'Member joined'

  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start gap-4">
        <span className="rounded-xl bg-lime-100 p-3 text-lime-900">
          <Icon
            aria-hidden="true"
            className="size-5"
          />
        </span>

        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-lime-800">
            {title}
          </p>

          <h2 className="mt-1 break-words text-lg font-bold">
            {item.memberName}
          </h2>

          <Link
            href={`/stokvels/${item.stokvelId}`}
            className="mt-2 inline-block break-words text-sm font-semibold text-slate-700 underline-offset-2 hover:underline"
          >
            {item.stokvelName}
          </Link>

          {item.amountInCents !== null && (
            <p className="mt-3 text-2xl font-extrabold">
              {formatCents(item.amountInCents)}
            </p>
          )}

          <p className="mt-3 text-sm text-slate-600">
            {formatDateTime(item.occurredAtUtc)}
          </p>
        </div>
      </div>
    </article>
  )
}
