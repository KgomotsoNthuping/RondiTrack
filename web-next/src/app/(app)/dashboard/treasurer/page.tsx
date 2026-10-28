
import Link from 'next/link'
import { Wallet } from 'lucide-react'

import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { StokvelCard } from '@/components/StokvelCard'
import { loadMemberHome } from '@/lib/stokvel-data'
import { stokvelsWhereTreasurer } from '@/lib/treasurer'

export default async function TreasurerDashboardPage() {
  const home = await loadMemberHome()

  if (home instanceof ApiError) {
    return <ApiErrorPanel error={home} />
  }

  const treasurerStokvels = stokvelsWhereTreasurer(
    home.stokvels,
  )

  return (
    <div className="space-y-8">
      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Treasurer overview
        </p>

        <h1 className="mt-2 text-3xl font-extrabold">
          Treasurer dashboard
        </h1>

        <p className="mt-2 text-slate-600">
          View stokvels where you have the Treasurer role.
        </p>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center gap-3">
          <span className="rounded-xl bg-lime-100 p-3 text-lime-900">
            <Wallet
              aria-hidden="true"
              className="size-6"
            />
          </span>

          <div>
            <p className="text-sm text-slate-600">
              Treasurer memberships
            </p>

            <p className="text-3xl font-extrabold">
              {treasurerStokvels.length}
            </p>
          </div>
        </div>
      </div>

      {treasurerStokvels.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <h2 className="text-xl font-bold">
            No treasurer memberships
          </h2>

          <p className="mx-auto mt-3 max-w-lg text-slate-600">
            You do not currently hold the Treasurer role
            in any of your stokvels. You can still use
            your member dashboard.
          </p>

          <Link
            href="/dashboard/member"
            className="mt-5 inline-flex min-h-11 items-center justify-center rounded-xl bg-lime-400 px-5 py-3 font-semibold text-lime-950 hover:bg-lime-300"
          >
            Go to member dashboard
          </Link>
        </section>
      ) : (
        <section aria-labelledby="treasurer-stokvels-heading">
          <h2
            id="treasurer-stokvels-heading"
            className="mb-5 text-xl font-bold"
          >
            Stokvels I manage
          </h2>

          <div className="grid gap-5 lg:grid-cols-2">
            {treasurerStokvels.map(stokvel => (
              <StokvelCard
                key={stokvel.stokvelId}
                stokvel={stokvel}
              />
            ))}
          </div>
        </section>
      )}
    </div>
  )
}
