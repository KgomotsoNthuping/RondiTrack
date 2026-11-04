'use client'

import type { ProofActionState } from '@/actions/proofs'

type Props = {
  state: ProofActionState
  dismissed: boolean
  onDismiss: () => void
}

export function ProofToast({
  state,
  dismissed,
  onDismiss,
}: Props) {
  if (
    dismissed ||
    !state.message ||
    !['success', 'unexpected'].includes(state.status)
  ) {
    return null
  }

  const success = state.status === 'success'

  return (
    <div
      role={success ? 'status' : 'alert'}
      aria-live={success ? 'polite' : 'assertive'}
      className={
        success
          ? 'fixed bottom-4 right-4 z-50 w-[min(24rem,calc(100vw-2rem))] rounded-xl border border-lime-400 bg-lime-50 p-5 shadow-xl'
          : 'fixed bottom-4 right-4 z-50 w-[min(24rem,calc(100vw-2rem))] rounded-xl border border-red-300 bg-red-50 p-5 shadow-xl'
      }
    >
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="font-bold">
            {success
              ? 'Proof updated'
              : 'Upload service error'}
          </p>

          <p className="mt-2 text-sm">
            {state.message}
          </p>

          {state.reference && (
            <p className="mt-3 break-all font-mono text-xs">
              Reference: {state.reference}
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss notification"
          className="min-h-11 min-w-11 rounded-lg border border-current px-2 text-sm font-semibold"
        >
          Close
        </button>
      </div>
    </div>
  )
}
