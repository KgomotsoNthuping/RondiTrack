import 'server-only'

import type { ContributionItem } from '@/api/types'
import { ApiError } from '@/api/ApiError'
import type { StreamResult } from '@/lib/settleForStream'
import { ContributionCard } from '@/components/ContributionCard'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'

type RecentContributionsProps = {
  request: Promise<
    StreamResult<ContributionItem[] | ApiError>
  >
  startedAt: number
}

export async function RecentContributions({
  request,
  startedAt,
}: RecentContributionsProps) {
  const outcome = await request

  if (!outcome.ok) {
    throw outcome.error
  }

  const result = outcome.value

  if (result instanceof ApiError) {
    return (
      <section
        aria-labelledby="recent-contributions-title"
        className="space-y-4"
      >
        <h2
          id="recent-contributions-title"
          className="text-xl font-bold"
        >
          Recent contributions
        </h2>

        <ApiErrorPanel error={result} />
      </section>
    )
  }

  console.info(
    `[RondiTrack] Recent contributions ready in ${Math.round(
      performance.now() - startedAt,
    )}ms`,
  )

  return (
    <section
      aria-labelledby="recent-contributions-title"
      className="space-y-5"
    >
      <header>
        <h2
          id="recent-contributions-title"
          className="text-xl font-bold"
        >
          Recent contributions
        </h2>

        <p className="mt-2 text-sm text-slate-600">
          Up to five contribution records available
          to your account.
        </p>
      </header>

      {result.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-6 text-center">
          <h3 className="font-semibold">
            No contributions yet
          </h3>

          <p className="mt-2 text-sm text-slate-600">
            Contribution records will appear here
            when payments have been recorded.
          </p>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {result.map(contribution => (
            <ContributionCard
              key={contribution.id}
              contribution={contribution}
            />
          ))}
        </div>
      )}
    </section>
  )
}

export function RecentContributionsSkeleton() {
  return (
    <section
      aria-label="Loading recent contributions"
      className="space-y-5"
    >
      <h2 className="text-xl font-bold">
        Recent contributions
      </h2>

      <div
        role="status"
        aria-live="polite"
        className="grid animate-pulse gap-4 lg:grid-cols-2"
      >
        <span className="sr-only">
          Loading contribution records...
        </span>

        {[1, 2, 3].map(item => (
          <div
            key={item}
            className="rounded-xl border border-slate-200 bg-white p-5"
          >
            <div className="flex items-start justify-between">
              <div className="space-y-3">
                <div className="h-4 w-24 rounded bg-slate-200" />
                <div className="h-7 w-36 rounded bg-slate-200" />
              </div>

              <div className="h-7 w-20 rounded-full bg-slate-200" />
            </div>

            <div className="mt-6 grid grid-cols-2 gap-4 border-t border-slate-200 pt-4">
              <div className="space-y-2">
                <div className="h-3 w-24 rounded bg-slate-200" />
                <div className="h-4 w-20 rounded bg-slate-200" />
              </div>

              <div className="space-y-2">
                <div className="h-3 w-24 rounded bg-slate-200" />
                <div className="h-4 w-28 rounded bg-slate-200" />
              </div>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
