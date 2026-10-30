import { Suspense } from 'react'
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { ArrowLeft, CalendarDays } from 'lucide-react'
import type { SearchParams } from 'nuqs/server'

import Loading from '@/app/(app)/loading'
import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { HistoryFilters } from '@/components/HistoryFilters'
import { StatusBadge } from '@/components/StatusBadge'
import { formatCents } from '@/lib/money'
import { formatDateTime } from '@/lib/dates'
import {
  getStokvelDetail,
  loadRecordableMembers,
} from '@/lib/stokvel-data'
import { loadHistoryFilters } from '@/lib/historyFilters'
import { southAfricanDaysToUtcRange } from '@/lib/southAfricanDates'
import { loadContributionHistoryPage } from '@/lib/history-data'

type HistoryPageProps = {
  params: Promise<{ id: string }>
  searchParams: Promise<SearchParams>
}

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

async function HistoryContent({
  params,
  searchParams,
}: HistoryPageProps) {
  const [{ id }, filters] = await Promise.all([
    params,
    loadHistoryFilters(searchParams),
  ])

  if (!uuidPattern.test(id)) {
    notFound()
  }

  const range = southAfricanDaysToUtcRange(
    filters.from,
    filters.to,
  )

  const [stokvel, members, history] =
    await Promise.all([
      getStokvelDetail(id),
      loadRecordableMembers(id),
      loadContributionHistoryPage(
        id,
        filters.member,
        range,
      ),
    ])

  if (members instanceof ApiError) {
    return <ApiErrorPanel error={members} />
  }

  if (history instanceof ApiError) {
    return <ApiErrorPanel error={history} />
  }

  const isFiltered =
    filters.member !== null ||
    filters.from !== null ||
    filters.to !== null

  return (
    <div className="space-y-7">
      <Link
        href={`/stokvels/${id}`}
        className="inline-flex min-h-11 items-center gap-2 font-semibold text-lime-800 hover:underline"
      >
        <ArrowLeft
          aria-hidden="true"
          className="size-4"
        />
        Back to stokvel details
      </Link>

      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Contribution history
        </p>

        <h1 className="mt-2 break-words text-3xl font-extrabold">
          {stokvel.name}
        </h1>

        <p className="mt-2 text-slate-600">
          Filter the payment records by member and
          South African calendar dates.
        </p>
      </header>

      <HistoryFilters members={members} />

      {range.isReversed && (
        <div
          role="status"
          className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"
        >
          The To date is earlier than the From date.
          Both date limits have been ignored.
          Choose a valid date range to apply them.
        </div>
      )}

      <section
        aria-labelledby="history-results-title"
        className="space-y-4"
      >
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2
            id="history-results-title"
            className="text-xl font-bold"
          >
            Contributions
          </h2>

          <p className="text-sm text-slate-600">
            Newest first
          </p>
        </div>

        {history.items.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
            <CalendarDays
              aria-hidden="true"
              className="mx-auto size-8 text-slate-400"
            />

            <h3 className="mt-3 text-lg font-semibold">
              {isFiltered
                ? 'No contributions match these filters'
                : 'No contributions recorded yet'}
            </h3>

            <p className="mt-2 text-sm text-slate-600">
              {isFiltered
                ? 'Change the selection above or use Clear filters to see everything.'
                : 'Contribution records will appear here when payments have been recorded.'}
            </p>
          </div>
        ) : (
          <div className="grid gap-4 lg:grid-cols-2">
            {history.items.map(item => (
              <article
                key={item.id}
                className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm text-slate-600">
                      {item.memberName}
                    </p>

                    <h3 className="mt-2 text-2xl font-bold">
                      {formatCents(
                        item.amountInCents,
                      )}
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
                        ? formatDateTime(
                            item.paidAtUtc,
                          )
                        : 'Not paid yet'}
                    </p>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}

        <p
          role="status"
          className="text-sm text-slate-600"
        >
          Showing {history.items.length}{' '}
          {history.items.length === 1
            ? 'contribution'
            : 'contributions'}
          {history.nextPageToken
            ? '. More records are available.'
            : '. End of available results.'}
        </p>
      </section>
    </div>
  )
}

export default function ContributionHistoryPage({
  params,
  searchParams,
}: HistoryPageProps) {
  return (
    <Suspense fallback={<Loading />}>
      <HistoryContent
        params={params}
        searchParams={searchParams}
      />
    </Suspense>
  )
}
