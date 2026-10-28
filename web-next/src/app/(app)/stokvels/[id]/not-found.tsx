import Link from 'next/link'
import { ArrowLeft, SearchX } from 'lucide-react'

export default function StokvelNotFound() {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
      <SearchX
        aria-hidden="true"
        className="mx-auto size-12 text-slate-500"
      />

      <h1 className="mt-5 text-2xl font-extrabold">
        Stokvel not found
      </h1>

      <p className="mx-auto mt-3 max-w-md text-slate-600">
        This stokvel may no longer exist, or you may
        no longer be a member.
      </p>

      <Link
        href="/stokvels"
        className="mt-6 inline-flex min-h-11 items-center gap-2 rounded-xl bg-lime-400 px-5 py-3 font-semibold text-lime-950 hover:bg-lime-300"
      >
        <ArrowLeft
          aria-hidden="true"
          className="size-4"
        />
        Back to my stokvels
      </Link>
    </section>
  )
}
