'use client'

import {
  useActionState,
  useRef,
  useState,
  type FormEvent,
} from 'react'

import {
  uploadProofAction,
  initialProofState,
} from '@/actions/proofs'
import { validateProofFile } from '@/lib/proofFiles'
import { ProofToast } from './ProofToast'

type Props = {
  stokvelId: string
  contributionId: string
  replacing: boolean
}

export function ProofUploadForm({
  stokvelId,
  contributionId,
  replacing,
}: Props) {
  const fileRef = useRef<HTMLInputElement>(null)

  const [clientError, setClientError] =
    useState<string | null>(null)

  const [selectionChanged, setSelectionChanged] =
    useState(false)

  const [dismissedResultId, setDismissedResultId] =
    useState<string | null>(null)

  const [state, formAction, pending] =
    useActionState(
      uploadProofAction,
      initialProofState,
    )

  const fileError =
    clientError ??
    (selectionChanged ? null : state.fileError)

  function validateSelection() {
    const file = fileRef.current?.files?.[0] ?? null
    const error = validateProofFile(file)

    setClientError(error)
    setSelectionChanged(true)
  }

  function checkSubmission(
    event: FormEvent<HTMLFormElement>,
  ) {
    const file = fileRef.current?.files?.[0] ?? null
    const error = validateProofFile(file)

    if (error) {
      event.preventDefault()
      setClientError(error)
      return
    }

    setClientError(null)
    setSelectionChanged(false)
  }

  return (
    <section
      aria-labelledby="proof-upload-title"
      className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <h2
        id="proof-upload-title"
        className="text-xl font-bold"
      >
        {replacing
          ? 'Replace proof of payment'
          : 'Upload proof of payment'}
      </h2>

      <p className="mt-2 text-sm text-slate-600">
        Choose a PDF, JPEG or PNG file.
        Maximum size: 5 MiB.
      </p>

      <form
        action={formAction}
        onSubmit={checkSubmission}
        className="mt-5 space-y-5"
      >
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

        <div className="space-y-2">
          <label
            htmlFor="proof-file"
            className="block text-sm font-semibold"
          >
            Proof document
          </label>

          <input
            ref={fileRef}
            id="proof-file"
            name="file"
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
            required
            disabled={pending}
            onChange={validateSelection}
            aria-invalid={Boolean(fileError)}
            aria-describedby={
              fileError
                ? 'proof-file-error'
                : 'proof-file-help'
            }
            className="block min-h-11 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm file:mr-4 file:rounded-lg file:border-0 file:bg-lime-100 file:px-4 file:py-2 file:font-semibold"
          />

          <p
            id="proof-file-help"
            className="text-sm text-slate-500"
          >
            Accepted: PDF, JPG and PNG.
          </p>

          {fileError && (
            <p
              id="proof-file-error"
              role="alert"
              className="text-sm font-medium text-red-700"
            >
              {fileError}
            </p>
          )}
        </div>

        {(state.status === 'conflict' ||
          state.status === 'missing') &&
          !selectionChanged && (
            <div
              role="alert"
              className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"
            >
              {state.message}
            </div>
          )}

        <button
          type="submit"
          disabled={pending}
          className="min-h-11 w-full rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300 disabled:cursor-wait disabled:opacity-60"
        >
          {pending
            ? 'Uploading proof...'
            : replacing
              ? 'Replace proof'
              : 'Upload proof'}
        </button>
      </form>

      <ProofToast
        state={state}
        dismissed={
          dismissedResultId === state.resultId
        }
        onDismiss={() =>
          setDismissedResultId(state.resultId)
        }
      />
    </section>
  )
}
