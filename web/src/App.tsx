import './styles/theme-overrides.css'
import { Wallet } from 'lucide-react'
import { StokvelCard } from './components/StokvelCard'
import { ContributionList } from './components/ContributionList'
import { DueCountdown } from './components/DueCountdown'
import { ArrearsBadge } from './components/StatusBadge'
import { ThemeProvider } from './components/ThemeProvider'
import { ThemeSwitcher } from './components/ThemeSwitcher'
import { home, contributions } from './data/sample'
import { cycles } from './data/cycles'

function Dashboard() {
  return <div className="min-h-screen min-w-0 bg-background text-foreground">
    <header className="w-full bg-lime-400 text-lime-950 shadow-sm">
      <div className="mx-auto flex min-h-20 max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <span className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-lime-950 text-lime-300">
            <Wallet aria-hidden="true" className="size-6" />
          </span>
          <div className="min-w-0"><h1 className="break-words text-xl font-extrabold sm:text-2xl">RondiTrack</h1>
          </div>
        </div>
        <ThemeSwitcher />
      </div>
    </header>
    <main className="mx-auto w-full max-w-6xl space-y-10 px-4 py-8 sm:px-6">
      <section aria-labelledby="stokvels-heading" className="space-y-4">
        <h2 id="stokvels-heading" className="text-xl font-bold">My Stokvels</h2>
        {home.length === 0 ? <p className="text-muted-foreground">No stokvels available.</p> :
          <div className="grid min-w-0 gap-5 md:grid-cols-2">
            {home.map(stokvel => <div key={stokvel.stokvelId} className="min-w-0 space-y-2 overflow-hidden rounded-xl border border-border bg-card p-3 text-card-foreground shadow-sm sm:p-4">
              <StokvelCard stokvel={stokvel} />
              <div className="flex min-w-0 flex-wrap items-center gap-3 border-t border-border pt-3">
                <DueCountdown dueDate={stokvel.cycleDueDate} status={stokvel.cycleStatus} />
                <ArrearsBadge amountInCents={stokvel.arrearsInCents} />
              </div>
            </div>)}
          </div>}
      </section>
      <ContributionList cycles={cycles} contributions={contributions} />
    </main>
  </div>
}
export default function App() {
  return <ThemeProvider><Dashboard /></ThemeProvider>
}
