
import { StokvelCard } from './components/StokvelCard'
import { ContributionCard } from './components/ContributionCard'
import { home, contributions } from './data/sample'

function App() {
  // Find the family stokvel whose contributions
  // are displayed below the dashboard.
  const familyStokvel = home.find(
    (stokvel) =>
      stokvel.name === 'Family Savers'
  )

  return (
    <main className="app">
      <header>
        <h1>RondiTrack</h1>
      </header>

      {/* STOKVEL DASHBOARD */}
      <section className="dashboard-section">
        <h2>My Stokvels</h2>

        {home.length === 0 ? (
          <p>No stokvels available.</p>
        ) : (
          <div className="stokvel-grid">
            {home.map((stokvel) => (
              <StokvelCard
                key={stokvel.stokvelId}
                stokvel={stokvel}
              />
            ))}
          </div>
        )}
      </section>

      {/* CONTRIBUTION LIST */}
      <section className="dashboard-section">
        <h2>
          Contributions
          {familyStokvel != null
            ? ` — ${familyStokvel.name}`
            : ''}
        </h2>

        {contributions.length === 0 ? (
          <p>No contributions recorded yet.</p>
        ) : (
          <div className="contribution-list">
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
  )
}

export default App
