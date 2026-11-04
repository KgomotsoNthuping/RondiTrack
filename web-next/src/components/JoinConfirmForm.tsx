'use client'

import { useActionState, useEffect, useRef } from 'react'
import Link from 'next/link'

import {
  joinWithInviteAction,
  type JoinActionState,
} from '@/actions/invites'
import { joinHref } from '@/lib/joinFlow'

const initialState: JoinActionState = {
  status: 'idle',
  message: null,
  stokvelId: null,
  reference: null,
}

export function JoinConfirmForm({
  code,
  stokvelName,
}: {
  code: string
  stokvelName: string
}) {
  const [state, formAction, pending] =
    useActionState(
      joinWithInviteAction,
      initialState,
    )

  const resultRef = useRef<HTMLHeadingElement>(null)

  const finished =
    state.status === 'joined' ||
    state.status === 'already-member'

  useEffect(() => {
    if (finished) {
      resultRef.current?.focus()
    }
  }, [finished])

  if (finished) {
    return (
      <section
        role="status"
        className="rounded-xl border border-lime-300 bg-lime-50 p-6"
      >
        <h2
          ref={resultRef}
          tabIndex={-1}
          className="text-xl font-bold text-lime-950 outline-none"
        >
          {state.status === 'joined'
            ? 'Membership confirmed'
            : 'Already a member'}
        </h2>

        <p className="mt-3 text-lime-950">
          {state.message}
        </p>

        <Link
          href={
            state.stokvelId
              ? `/stokvels/${state.stokvelId}`
              : '/stokvels'
          }
          className="mt-5 inline-flex min-h-11 items-center rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300"
        >
          Open stokvel
        </Link>
      </section>
    )
  }

  return (
    <div className="space-y-5">
      <p className="text-slate-700">
        You are about to join{' '}
        <strong>{stokvelName}</strong>.
        Confirm that you have reviewed the rules,
        monthly contribution and schedule.
      </p>

      {state.message && (
        <div
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-950"
        >
          <p>{state.message}</p>

          {state.reference && (
            <p className="mt-2 break-all font-mono text-xs">
              Reference: {state.reference}
            </p>
          )}

          {(state.status === 'unknown' ||
            state.status === 'expired') && (
            <Link
              href={joinHref(code, 'code')}
              className="mt-3 inline-block font-semibold underline"
            >
              Return to invitation code
            </Link>
          )}
        </div>
      )}

      <form action={formAction}>
        <input
          type="hidden"
          name="code"
          value={code}
        />

        <button
          type="submit"
          disabled={pending}
          className="min-h-11 w-full rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300 disabled:cursor-wait disabled:opacity-60"
        >
          {pending
            ? 'Joining stokvel...'
            : `Confirm and join ${stokvelName}`}
        </button>
      </form>
    </div>
  )
}
