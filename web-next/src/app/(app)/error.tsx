'use client'

import Link from 'next/link'
import { useEffect } from 'react'
import {
  AlertCircle,
  ArrowLeft,
  RefreshCw,
} from 'lucide-react'

type AppErrorProps = {
  error: Error & { digest?: string }
  reset: () => void
}

export default function AppError({
  error,
  reset,
}: AppErrorProps) {
  useEffect(() => {
    console.error('RondiTrack route error', error)
  }, [error])

  return (
    <section
      role="alert"
      className="rounded-2xl border border-red-200 bg-red-50 p-6 text-red-950"
    >
      <div className="flex items-start gap-3">
        <AlertCircle
          aria-hidden="true"
          className="mt-1 size-6 shrink-0"
        />

        <div className="min-w-0 flex-1">
          <h1 className="text-xl font-bold">
            Something went wrong
          </h1>

          <p className="mt-3 text-sm">
            RondiTrack could not load this information.
            Please try again.
          </p>

          {error.digest && (
            <p className="mt-4 break-all text-xs">
              Error reference: {error.digest}
            </p>
          )}

          <div className="mt-6 flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => reset()}
              className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-red-900 px-5 py-3 font-semibold text-white hover:bg-red-800"
            >
              <RefreshCw
                aria-hidden="true"
                className="size-4"
              />
              Try again
            </button>

            <Link
              href="/dashboard/member"
              className="inline-flex min-h-11 items-center gap-2 rounded-xl border border-red-300 px-5 py-3 font-semibold hover:bg-red-100"
            >
              <ArrowLeft
                aria-hidden="true"
                className="size-4"
              />
              Member dashboard
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
