'use client'

import { useActionState, useState } from 'react'
import { Wallet } from 'lucide-react'

import {
  recordContributionAction,
  type RecordContributionState,
} from '@/actions/contributions'
import { PaymentIntentTracker } from '@/contributions/paymentIntent'
import {
  canSelectContributionMember,
  type RecordableMember,
} from '@/lib/contributionMembers'
import { parseRandsToCents } from '@/lib/money'

type Props = {
  stokvelId: string
  cycleId: string
  cycleNumber: number
  currentUserId: string
  currentUserName: string
  memberRole: string
  members: RecordableMember[]
  initialKey: string
}

const initialState: RecordContributionState = {
  status: 'idle',
  attemptKey: null,
  nextKey: null,
  message: null,
  reference: null,
  fieldErrors: {},
}

export function RecordContributionForm({
  stokvelId,
  cycleId,
  cycleNumber,
  currentUserId,
  currentUserName,
  memberRole,
  members,
  initialKey,
}: Props) {
  const [tracker] = useState(
    () => new PaymentIntentTracker(initialKey),
  )

  const [draft, setDraft] = useState({
    amount: '',
    memberId: currentUserId,
    key: initialKey,
  })

  const [state, formAction, pending] = useActionState(
    recordContributionAction,
    initialState,
  )

  const canChooseMember =
    canSelectContributionMember(memberRole)

  const completed =
    state.status === 'success' &&
    state.attemptKey === draft.key &&
    state.nextKey !== null

  const currentKey =
    completed && state.nextKey
      ? state.nextKey
      : draft.key

  const amount = completed ? '' : draft.amount

  const selectedMemberId = completed
    ? currentUserId
    : canChooseMember
      ? draft.memberId
      : currentUserId

  const showErrors =
    (state.status === 'error' ||
      state.status === 'uncertain') &&
    state.attemptKey === currentKey

  const amountErrors = showErrors
    ? state.fieldErrors.amount ?? []
    : []

  const memberErrors = showErrors
    ? state.fieldErrors.userId ?? []
    : []

  const availableMembers = members.some(
    member => member.id === currentUserId,
  )
    ? members
    : [
        {
          id: currentUserId,
          fullName: currentUserName,
        },
        ...members,
      ]

  const recordedMemberName =
    availableMembers.find(
      member => member.id === draft.memberId,
    )?.fullName ?? currentUserName

  function updateDraft(
    nextAmount: string,
    nextMemberId: string,
  ) {
    if (completed && state.nextKey) {
      tracker.reset(state.nextKey)
    }

    const cents = parseRandsToCents(nextAmount)

    const nextKey = tracker.keyFor({
      cycleId,
      userId: nextMemberId,
      amountInCents: cents ?? -1,
    })

    setDraft({
      amount: nextAmount,
      memberId: nextMemberId,
      key: nextKey,
    })
  }

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
          className="mb-5 rounded-xl border border-lime-300 bg-lime-50 p-4 text-sm text-lime-950"
        >
          <p className="font-semibold">
            {state.message}
          </p>

          <p className="mt-1">
            Member: {recordedMemberName}
          </p>
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
          name="idempotencyKey"
          value={currentKey}
        />

        <div className="space-y-2">
          {canChooseMember ? (
            <>
              <label
                htmlFor="contribution-user"
                className="block text-sm font-semibold text-slate-900"
              >
                Member
              </label>

              <select
                id="contribution-user"
                name="userId"
                value={selectedMemberId}
                onChange={event =>
                  updateDraft(
                    amount,
                    event.target.value,
                  )
                }
                disabled={pending}
                aria-invalid={memberErrors.length > 0}
                aria-describedby={
                  memberErrors.length > 0
                    ? 'contribution-user-error'
                    : undefined
                }
                className="min-h-11 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none focus-visible:ring-2 focus-visible:ring-lime-500 disabled:bg-slate-100"
              >
                <option value="">
                  Select member
                </option>

                {availableMembers.map(member => (
                  <option
                    key={member.id}
                    value={member.id}
                  >
                    {member.fullName}
                  </option>
                ))}
              </select>
            </>
          ) : (
            <>
              <p className="block text-sm font-semibold text-slate-900">
                Member
              </p>

              <p className="rounded-lg border border-slate-300 bg-slate-50 px-3 py-3 text-sm text-slate-900">
                {currentUserName}
              </p>

              <input
                type="hidden"
                name="userId"
                value={currentUserId}
              />
            </>
          )}

          {memberErrors.length > 0 && (
            <div
              id="contribution-user-error"
              role="alert"
              className="space-y-1 text-sm text-red-700"
            >
              {memberErrors.map((message, index) => (
                <p key={index}>{message}</p>
              ))}
            </div>
          )}
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
              updateDraft(
                event.target.value,
                selectedMemberId,
              )
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
