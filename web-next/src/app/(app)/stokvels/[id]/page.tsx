import Link from 'next/link'
import { notFound } from 'next/navigation'
import {
  ArrowLeft,
  CalendarDays,
  Users,
  Wallet,
} from 'lucide-react'

import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { StatusBadge } from '@/components/StatusBadge'
import { loadStokvelDetail } from '@/lib/stokvel-data'
import { formatCents } from '@/lib/money'
import { formatCalendarDate } from '@/lib/dates'
import { roleLabel } from '@/lib/roles'

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

type StokvelDetailPageProps = {
  params: Promise<{
    id: string
  }>
}

export default async function StokvelDetailPage({
  params,
}: StokvelDetailPageProps) {
  const { id } = await params

  if (!uuidPattern.test(id)) {
    notFound()
  }

  const stokvel = await loadStokvelDetail(id)

  if (stokvel instanceof ApiError) {
    return <ApiErrorPanel error={stokvel} />
  }

  const cycle = stokvel.currentCycle

  return (
    <div className="space-y-7">
      <Link
        href="/stokvels"
        className="inline-flex min-h-11 items-center gap-2 font-semibold text-lime-800 hover:underline"
      >
        <ArrowLeft
          aria-hidden="true"
          className="size-4"
        />
        Back to my stokvels
      </Link>

      <header className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start gap-4">
          <span className="rounded-xl bg-lime-100 p-3 text-lime-900">
            <Wallet
              aria-hidden="true"
              className="size-7"
            />
          </span>

          <div className="min-w-0 flex-1">
            <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
              Stokvel details
            </p>

            <h1 className="mt-1 break-words text-3xl font-extrabold">
              {stokvel.name}
            </h1>

            <p className="mt-2 text-slate-600">
              Your role: {roleLabel(stokvel.role)}
            </p>
          </div>
        </div>
      </header>

      <section
        aria-label="Stokvel summary"
        className="grid gap-4 sm:grid-cols-3"
      >
        <InfoCard
          label="Monthly contribution"
          value={formatCents(
            stokvel.monthlyContributionInCents,
          )}
        />

        <InfoCard
          label="Members"
          value={String(stokvel.memberCount)}
        />

        <InfoCard
          label="Progress"
          value={`Period ${stokvel.currentPeriod} of ${stokvel.totalPeriods}`}
        />
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-2">
          <CalendarDays
            aria-hidden="true"
            className="size-5 text-lime-700"
          />

          <h2 className="text-xl font-bold">
            Current contribution cycle
          </h2>
        </div>

        {cycle === null ? (
          <p className="mt-4 text-slate-600">
            No contribution cycle is currently available.
          </p>
        ) : (
          <div className="mt-5 space-y-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className="text-lg font-semibold">
                Cycle {cycle.cycleNumber}
              </h3>

              <StatusBadge
                kind="cycle"
                status={cycle.status}
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <InfoCard
                label="Target amount"
                value={formatCents(
                  cycle.targetAmountInCents,
                )}
              />

              <InfoCard
                label="Total collected"
                value={formatCents(
                  cycle.totalCollectedInCents,
                )}
              />

              <InfoCard
                label="Amount outstanding"
                value={formatCents(
                  cycle.amountOutstandingInCents,
                )}
              />

              <InfoCard
                label="Due date"
                value={
                  cycle.dueDate
                    ? formatCalendarDate(cycle.dueDate)
                    : 'Not scheduled'
                }
              />
            </div>
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center gap-2">
          <Users
            aria-hidden="true"
            className="size-5 text-lime-700"
          />

          <h2 className="text-xl font-bold">
            Membership information
          </h2>
        </div>

        <p className="mt-4 text-slate-700">
          This stokvel has{' '}
          <strong>{stokvel.memberCount}</strong>{' '}
          {stokvel.memberCount === 1
            ? 'member'
            : 'members'}.
        </p>

        <p className="mt-2 text-slate-700">
          Your membership role is{' '}
          <strong>{roleLabel(stokvel.role)}</strong>.
        </p>

        <h3 className="mt-5 font-semibold">
          Stokvel rules
        </h3>

        <p className="mt-2 whitespace-pre-wrap text-slate-600">
          {stokvel.rules?.trim() ||
            'No rules have been provided.'}
        </p>
      </section>
    </div>
  )
}

function InfoCard({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <p className="text-sm text-slate-600">
        {label}
      </p>

      <p className="mt-2 break-words text-xl font-bold">
        {value}
      </p>
    </div>
  )
}
