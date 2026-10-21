import { Wallet } from 'lucide-react'
import { StokvelCard } from './components/StokvelCard'
import { ContributionList } from './components/ContributionList'
import { home, contributions } from './data/sample'
import { cycles } from './data/cycles'

function App() {
  return (
    <div className="min-h-screen min-w-0 bg-background text-foreground">
      <header className="w-full bg-lime-400 text-lime-950 shadow-sm">
        <div className="mx-auto flex min-h-20 max-w-6xl items-center gap-3 px-4 sm:px-6">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-lime-950 text-lime-300">
            <Wallet aria-hidden="true" className="size-6" />
          </div>
          <div className="min-w-0">
            <h1 className="text-xl font-extrabold tracking-tight sm:text-2xl">RondiTrack</h1>
            <p className="text-sm font-medium">Your stokvels, all in one place</p>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl space-y-10 px-4 py-8 sm:px-6">
        <section className="space-y-4">
          <h2 className="text-xl font-bold">My Stokvels</h2>
          <div className="grid min-w-0 gap-5 md:grid-cols-2">
            {home.map(stokvel => <div key={stokvel.stokvelId} className="min-w-0"><StokvelCard stokvel={stokvel} /></div>)}
            {home.length === 0 && <p>No stokvels available.</p>}
          </div>
        </section>
        <ContributionList cycles={cycles} contributions={contributions} />
      </main>
    </div>
  )
}
export default App
