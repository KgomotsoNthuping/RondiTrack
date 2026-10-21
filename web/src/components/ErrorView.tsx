
import { AlertCircle, ArrowLeft, RefreshCw } from 'lucide-react'
import { describeError } from '../lib/errorMessages'

type ErrorViewProps = {
  error: unknown
  onRetry?: () => void
  onBack?: () => void
}

export function ErrorView({
  error,
  onRetry,
  onBack,
}: ErrorViewProps) {
  const description = describeError(error)

  return (
    <section
      role="alert"
      className="rounded-2xl border border-red-200 bg-red-50 p-5 text-red-950 shadow-sm dark:border-red-800 dark:bg-red-950 dark:text-red-100 sm:p-6"
    >
      <div className="flex items-start gap-3">
        <AlertCircle
          aria-hidden="true"
          className="mt-0.5 size-6 shrink-0"
        />

        <div className="min-w-0 flex-1">
          <h2 className="text-lg font-bold">
            {description.title}
          </h2>

          <p className="mt-2 text-sm">
            {description.message}
          </p>

          {Object.keys(description.fieldErrors).length > 0 && (
            <div className="mt-3 space-y-2">
              {Object.entries(description.fieldErrors).map(
                ([field, messages]) => (
                  <div key={field}>
                    <p className="text-sm font-semibold">
                      {field}
                    </p>

                    <ul className="list-inside list-disc text-sm">
                      {messages.map((message, index) => (
                        <li key={index}>{message}</li>
                      ))}
                    </ul>
                  </div>
                ),
              )}
            </div>
          )}

          {description.reference && (
            <p className="mt-4 break-all text-xs">
              Reference:{' '}
              <code className="select-text rounded bg-black/10 px-2 py-1 font-mono dark:bg-white/10">
                {description.reference}
              </code>
            </p>
          )}

          <div className="mt-5 flex flex-wrap gap-3">
            {onBack && (
              <button
                type="button"
                onClick={onBack}
                className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-current px-4 py-2 text-sm font-semibold"
              >
                <ArrowLeft aria-hidden="true" className="size-4" />
                Back to my stokvels
              </button>
            )}

            {description.canRetry && onRetry && (
              <button
                type="button"
                onClick={onRetry}
                className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-red-900 px-4 py-2 text-sm font-semibold text-white hover:bg-red-800 dark:bg-red-200 dark:text-red-950"
              >
                <RefreshCw aria-hidden="true" className="size-4" />
                Try again
              </button>
            )}
          </div>
        </div>
      </div>
    </section>
  )
}
