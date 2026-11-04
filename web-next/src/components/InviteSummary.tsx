import type { InvitePreview } from '@/lib/inviteTypes'
import { formatCents } from '@/lib/money'
import { formatCalendarDate } from '@/lib/dates'

export function InviteSummary({
  preview,
}: {
  preview: InvitePreview
}) {
  return (
    <section
      aria-labelledby="invite-summary-title"
      className="space-y-5"
    >
      <div>
        <h2
          id="invite-summary-title"
          className="text-xl font-bold"
        >
          {preview.name}
        </h2>
        <p className="mt-2 text-sm text-slate-600">
          Review what membership involves.
        </p>
      </div>

      <dl className="grid gap-3 sm:grid-cols-2">
        <div className="rounded-xl bg-lime-50 p-4">
          <dt className="text-sm text-lime-800">
            Monthly contribution
          </dt>
          <dd className="mt-2 text-2xl font-extrabold text-lime-950">
            {formatCents(
              preview.contributionAmountInCents,
            )}
          </dd>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <dt className="text-sm text-slate-600">
            Members
          </dt>
          <dd className="mt-2 text-xl font-bold">
            {preview.memberCount}
          </dd>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <dt className="text-sm text-slate-600">
            Contribution schedule
          </dt>
          <dd className="mt-2 font-semibold">
            Period {preview.currentPeriod} of{' '}
            {preview.totalPeriods}
          </dd>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <dt className="text-sm text-slate-600">
            Next due date
          </dt>
          <dd className="mt-2 font-semibold">
            {preview.nextContributionDueDate
              ? formatCalendarDate(
                  preview.nextContributionDueDate,
                )
              : 'Not scheduled'}
          </dd>
        </div>
      </dl>

      <div className="rounded-xl border border-slate-200 p-5">
        <h3 className="font-bold">
          Stokvel rules
        </h3>

        <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-700">
          {preview.rules?.trim() ||
            'No additional rules have been provided.'}
        </p>
      </div>
    </section>
  )
}
