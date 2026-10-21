
import { StokvelCard } from './components/StokvelCard'
import { ContributionCard } from './components/ContributionCard'
import { home, contributions } from './data/sample'
import { Wallet } from 'lucide-react'

function App() {
  const familyStokvel = home.find(
    (stokvel) => stokvel.name === 'Family Savers'
  )

  return (
    <div className="min-h-screen bg-lime-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">

      <header className="w-full bg-lime-400 text-lime-950 shadow-sm">
        <div className="mx-auto flex min-h-20 max-w-6xl items-center gap-3 px-4 sm:px-6">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-lime-950 text-lime-300">
            <Wallet aria-hidden="true" className="size-6" />
          </div>

          <div>
            <h1 className="text-xl font-extrabold tracking-tight sm:text-2xl">
              RondiTrack
            </h1>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl space-y-10 px-4 py-8 sm:px-6">

        <section className="space-y-4">
          <h2 className="text-xl font-bold tracking-tight">
            My Stokvels
          </h2>

          {home.length === 0 ? (
            <p className="text-slate-600 dark:text-slate-300">
              No stokvels available.
            </p>
          ) : (
            <div className="grid min-w-0 gap-5 md:grid-cols-2">
              {home.map((stokvel) => (
                <StokvelCard
                  key={stokvel.stokvelId}
                  stokvel={stokvel}
                />
              ))}
            </div>
          )}
        </section>

        <section className="space-y-4">
          <h2 className="text-xl font-bold tracking-tight">
            Contributions
            {familyStokvel != null
              ? ` — ${familyStokvel.name}`
              : ''}
          </h2>

          {contributions.length === 0 ? (
            <p className="text-slate-600 dark:text-slate-300">
              No contributions recorded yet.
            </p>
          ) : (
            <div className="grid min-w-0 gap-4">
              {contributions.map((contribution) => (
                <ContributionCard
                  key={contribution.id}
                  contribution={contribution}
                />
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  )
}

export default App
