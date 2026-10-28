export default function HomePage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <section className="w-full max-w-xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <div className="mb-5 inline-flex rounded-xl bg-lime-100 p-3 text-lime-900">
          <span className="text-xl font-bold">R</span>
        </div>
        <h1 className="text-3xl font-bold tracking-tight">
          RondiTrack
        </h1>
        <p className="mt-3 text-slate-600">
          Manage stokvel memberships, contribution cycles,
          payment records and financial summaries in one place.
        </p>
        <p className="mt-6 border-t border-slate-200 pt-4 text-sm text-slate-500">
          The application foundation is configured.
        </p>
      </section>
    </main>
  )
}
