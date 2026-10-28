import 'server-only'

import Link from 'next/link'
import { AlertCircle } from 'lucide-react'
import { ApiError } from '@/api/ApiError'
import { describeError } from '@/lib/errorMessages'
import { RetryButton } from './RetryButton'

type ApiErrorPanelProps = {
  error: ApiError
}

export function ApiErrorPanel({
  error,
}: ApiErrorPanelProps) {
  const description = describeError(error)

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
          <h2 className="text-xl font-bold">
            {description.title}
          </h2>

          <p className="mt-3 text-sm">
            {description.message}
          </p>

          {description.reference && (
            <p className="mt-4 break-all text-xs">
              Reference:{' '}
              <code className="font-mono">
                {description.reference}
              </code>
            </p>
          )}

          <div className="mt-6 flex flex-wrap gap-3">
            {description.canRetry && <RetryButton />}

            <Link
              href="/dashboard/member"
              className="inline-flex min-h-11 items-center rounded-xl border border-red-300 px-5 py-3 font-semibold hover:bg-red-100"
            >
              Member dashboard
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
