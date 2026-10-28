import { StokvelCard } from '@/components/StokvelCard'
import { getMemberHome } from '@/lib/stokvel-data'

export default async function StokvelsPage() {
  const home = await getMemberHome()
  const stokvels = home.stokvels

  return (
    <div className="space-y-7">
      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Memberships
        </p>

        <h1 className="mt-2 text-3xl font-extrabold">
          My stokvels
        </h1>

        <p className="mt-2 text-slate-600">
          All stokvels you belong to, including your
          role and current contribution cycle.
        </p>
      </header>

      {stokvels.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <h2 className="text-xl font-bold">
            You have no stokvels yet
          </h2>

          <p className="mt-2 text-slate-600">
            Your memberships will appear here once
            they have been assigned to your account.
          </p>
        </section>
      ) : (
        <>
          <p className="text-sm text-slate-600">
            {stokvels.length}{' '}
            {stokvels.length === 1
              ? 'membership'
              : 'memberships'}
          </p>

          <div className="grid gap-5 lg:grid-cols-2">
            {stokvels.map(stokvel => (
              <StokvelCard
                key={stokvel.stokvelId}
                stokvel={stokvel}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}
