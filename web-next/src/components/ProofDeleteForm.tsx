'use client'

import { useActionState, useState } from 'react'

import {
  deleteProofAction,
  initialProofState,
} from '@/actions/proofs'
import { ProofToast } from './ProofToast'

type Props = {
  stokvelId: string
  contributionId: string
}

export function ProofDeleteForm({
  stokvelId,
  contributionId,
}: Props) {
  const [state, formAction, pending] =
    useActionState(
      deleteProofAction,
      initialProofState,
    )

  const [dismissedResultId, setDismissedResultId] =
    useState<string | null>(null)

  return (
    <div className="space-y-3">
      <form action={formAction}>
        <input
          type="hidden"
          name="stokvelId"
          value={stokvelId}
        />

        <input
          type="hidden"
          name="contributionId"
          value={contributionId}
        />

        <button
          type="submit"
          disabled={pending}
          className="min-h-11 rounded-xl border border-red-300 px-5 py-3 font-semibold text-red-800 hover:bg-red-50 disabled:cursor-wait disabled:opacity-60"
        >
          {pending
            ? 'Removing proof...'
            : 'Remove proof'}
        </button>
      </form>

      {(state.status === 'conflict' ||
        state.status === 'missing') && (
        <p
          role="alert"
          className="text-sm text-red-700"
        >
          {state.message}
        </p>
      )}

      <ProofToast
        state={state}
        dismissed={
          dismissedResultId === state.resultId
        }
        onDismiss={() =>
          setDismissedResultId(state.resultId)
        }
      />
    </div>
  )
}
