'use client'

import { useActionState, useState } from 'react'
import { Wallet } from 'lucide-react'

import {
  recordContributionAction,
  type RecordContributionState,
} from '@/actions/contributions'

import { PaymentIntentTracker } from '@/contributions/paymentIntent'
import { parseRandsToCents } from '@/lib/money'

type Props = {
  stokvelId: string
  cycleId: string
  cycleNumber: number
  currentUserId: string
  currentUserName: string
  initialKey: string
}

export function RecordContributionForm({
  stokvelId,
  cycleId,
  cycleNumber,
  currentUserId,
  currentUserName,
  initialKey,
}: Props) {
  const [tracker] = useState(
    () => new PaymentIntentTracker(initialKey),
  )

  const [draft, setDraft] = useState({
    amount: '',
    key: initialKey,
  })

  const initialState: RecordContributionState = {
    status: 'idle',
    attemptKey: null,
    nextKey: null,
    message: null,
    reference: null,
    fieldErrors: {},
  }

  const [state, formAction, pending] = useActionState(
    recordContributionAction,
    initialState,
  )

  /*
   * A successful action provides a fresh key.
   * Deriving the displayed values here avoids
   * resetting the form after an unsuccessful POST.
   */
  const completed =
    state.status === 'success' &&
    state.attemptKey === draft.key &&
    state.nextKey !== null

  const currentKey = completed
    ? state.nextKey!
    : draft.key

  const amount = completed ? '' : draft.amount

  const showErrors =
    (state.status === 'error' ||
      state.status === 'uncertain') &&
    state.attemptKey === currentKey

  function changeAmount(nextAmount: string) {
    if (completed && state.nextKey) {
      tracker.reset(state.nextKey)
    }

    const cents = parseRandsToCents(nextAmount)

    const nextKey = tracker.keyFor({
      cycleId,
      userId: currentUserId,
      amountInCents: cents ?? -1,
    })

    setDraft({
      amount: nextAmount,
      key: nextKey,
    })
  }

  const amountErrors = showErrors
    ? state.fieldErrors.amount ?? []
    : []

  return (
    <section
      aria-labelledby="record-contribution-title"
      className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <div className="mb-6 flex items-center gap-3">
        <span className="rounded-xl bg-lime-100 p-3 text-lime-900">
          <Wallet
            aria-hidden="true"
            className="size-5"
          />
        </span>

        <div>
          <h2
            id="record-contribution-title"
            className="text-xl font-bold"
          >
            Record contribution
          </h2>

          <p className="text-sm text-slate-600">
            Cycle {cycleNumber}
          </p>
        </div>
      </div>

      {completed && state.message && (
        <div
          role="status"
          className="mb-5 rounded-xl border border-lime-300 bg-lime-50 p-4 text-sm font-semibold text-lime-950"
        >
          {state.message}
        </div>
      )}

      {showErrors && state.message && (
        <div
          role="alert"
          className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-950"
        >
          <p>{state.message}</p>

          {state.reference && (
            <p className="mt-2 break-all font-mono text-xs">
              Reference: {state.reference}
            </p>
          )}
        </div>
      )}

      <form
        action={formAction}
        noValidate
        className="space-y-5"
      >
        <input
          type="hidden"
          name="stokvelId"
          value={stokvelId}
        />

        <input
          type="hidden"
          name="cycleId"
          value={cycleId}
        />

        <input
          type="hidden"
          name="userId"
          value={currentUserId}
        />

        <input
          type="hidden"
          name="idempotencyKey"
          value={currentKey}
        />

        <div className="space-y-2">
          <p className="block text-sm font-semibold text-slate-900">
            Member
            </p>

          <p className="rounded-lg border border-slate-300 bg-slate-50 px-3 py-3 text-sm text-slate-900">
            {currentUserName}
          </p>

          <p className="text-xs text-slate-500">
            This form currently records your own
            contributions.
          </p>
        </div>

        <div className="space-y-2">
          <label
            htmlFor="contribution-amount"
            className="block text-sm font-semibold text-slate-900"
          >
            Amount (rands)
          </label>

          <input
            id="contribution-amount"
            name="amount"
            type="text"
            inputMode="decimal"
            autoComplete="off"
            placeholder="500.00"
            value={amount}
            onChange={event =>
              changeAmount(event.target.value)
            }
            disabled={pending}
            required
            aria-invalid={amountErrors.length > 0}
            aria-describedby={
              amountErrors.length > 0
                ? 'contribution-amount-error'
                : undefined
            }
            className="min-h-11 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none focus-visible:ring-2 focus-visible:ring-lime-500 disabled:bg-slate-100"
          />

          {amountErrors.length > 0 && (
            <div
              id="contribution-amount-error"
              role="alert"
              className="space-y-1 text-sm text-red-700"
            >
              {amountErrors.map((message, index) => (
                <p key={index}>{message}</p>
              ))}
            </div>
          )}
        </div>

        {showErrors &&
          state.fieldErrors.userId?.map(
            (message, index) => (
              <p
                key={index}
                role="alert"
                className="text-sm text-red-700"
              >
                {message}
              </p>
            ),
          )}

        <button
          type="submit"
          disabled={pending}
          className="flex min-h-11 w-full items-center justify-center rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pending
            ? 'Recording contribution...'
            : 'Record contribution'}
        </button>
      </form>
    </section>
  )
}
