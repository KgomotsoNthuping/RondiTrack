export default function Loading() {
  return (
    <div
      role="status"
      aria-live="polite"
      className="animate-pulse space-y-6"
    >
      <span className="sr-only">
        Loading RondiTrack information...
      </span>

      <div className="space-y-3">
        <div className="h-4 w-32 rounded bg-slate-200" />
        <div className="h-9 w-64 max-w-full rounded bg-slate-200" />
        <div className="h-4 w-full max-w-lg rounded bg-slate-200" />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        {[1, 2, 3].map(item => (
          <div
            key={item}
            className="h-32 rounded-2xl border border-slate-200 bg-white p-5"
          >
            <div className="h-5 w-24 rounded bg-slate-200" />
            <div className="mt-5 h-8 w-32 rounded bg-slate-200" />
          </div>
        ))}
      </div>

      <div className="grid gap-5 lg:grid-cols-2">
        {[1, 2].map(item => (
          <div
            key={item}
            className="h-64 rounded-2xl border border-slate-200 bg-white"
          />
        ))}
      </div>
    </div>
  )
}
