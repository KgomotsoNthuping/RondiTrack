
import Link from 'next/link'
import {
  CircleDollarSign,
  Clock3,
  Wallet,
} from 'lucide-react'

import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { StokvelCard } from '@/components/StokvelCard'
import { loadMemberHome } from '@/lib/stokvel-data'
import { formatCents } from '@/lib/money'

export default async function MemberDashboardPage() {
  const home = await loadMemberHome()

  if (home instanceof ApiError) {
    return <ApiErrorPanel error={home} />
  }

  const stokvels = home.stokvels

  const openCycles = stokvels.filter(
    stokvel =>
      stokvel.cycleStatus === 'Open' &&
      stokvel.currentCycle !== null,
  )

  const totalPaid = openCycles.reduce(
    (total, stokvel) =>
      total + (stokvel.paidThisCycleInCents ?? 0),
    0,
  )

  const totalOwed = openCycles.reduce(
    (total, stokvel) =>
      total + (stokvel.stillOwedInCents ?? 0),
    0,
  )

  return (
    <div className="space-y-8">
      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Member overview
        </p>

        <h1 className="mt-2 text-3xl font-extrabold">
          Member dashboard
        </h1>

        <p className="mt-2 text-slate-600">
          View your stokvel memberships, contributions
          and upcoming payouts.
        </p>
      </header>

      <div className="grid gap-4 sm:grid-cols-3">
        <SummaryCard
          label="My stokvels"
          value={String(stokvels.length)}
          icon={Wallet}
        />

        <SummaryCard
          label="Paid in open cycles"
          value={formatCents(totalPaid)}
          icon={CircleDollarSign}
        />

        <SummaryCard
          label="Owed in open cycles"
          value={formatCents(totalOwed)}
          icon={Clock3}
        />
      </div>

      <section aria-labelledby="memberships-heading">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <h2
            id="memberships-heading"
            className="text-xl font-bold"
          >
            My memberships
          </h2>

          <Link
            href="/stokvels"
            className="font-semibold text-lime-800 underline-offset-4 hover:underline"
          >
            View all stokvels
          </Link>
        </div>

        {stokvels.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
            <h3 className="text-lg font-bold">
              No stokvel memberships yet
            </h3>

            <p className="mt-2 text-slate-600">
              Your stokvels will appear here once you
              become a member.
            </p>
          </div>
        ) : (
          <div className="grid gap-5 lg:grid-cols-2">
            {stokvels.map(stokvel => (
              <StokvelCard
                key={stokvel.stokvelId}
                stokvel={stokvel}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

type SummaryCardProps = {
  label: string
  value: string
  icon: typeof Wallet
}

function SummaryCard({
  label,
  value,
  icon: Icon,
}: SummaryCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <Icon
        aria-hidden="true"
        className="mb-4 size-6 text-lime-700"
      />

      <p className="text-sm text-slate-600">
        {label}
      </p>

      <p className="mt-2 break-words text-2xl font-extrabold">
        {value}
      </p>
    </div>
  )
}
