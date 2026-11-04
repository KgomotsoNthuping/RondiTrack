'use client'

import { useActionState, useState } from 'react'
import {
  createStokvelInviteAction,
  type CreateInviteState,
} from '@/actions/invites'
import { joinHref } from '@/lib/joinFlow'

const initialState: CreateInviteState = {
  status: 'idle',
  code: null,
  expiresAtUtc: null,
  message: null,
  reference: null,
}

export function CreateStokvelInvite({
  stokvelId,
}: {
  stokvelId: string
}) {
  const [state, formAction, pending] =
    useActionState(
      createStokvelInviteAction,
      initialState,
    )

  const [copied, setCopied] = useState(false)

  const link = state.code
    ? joinHref(state.code, 'code')
    : null

  async function copyLink() {
    if (!link) {
      return
    }

    try {
      await navigator.clipboard.writeText(
        `${window.location.origin}${link}`,
      )

      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  return (
    <section
      aria-labelledby="create-invite-title"
      className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <h2
        id="create-invite-title"
        className="text-xl font-bold"
      >
        Invite a member
      </h2>

      <p className="mt-2 text-sm text-slate-600">
        Create a secure invitation that expires
        after seven days.
      </p>

      <form
        action={formAction}
        className="mt-5"
      >
        <input
          type="hidden"
          name="stokvelId"
          value={stokvelId}
        />

        <button
          type="submit"
          disabled={pending}
          className="min-h-11 rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300 disabled:opacity-60"
        >
          {pending
            ? 'Creating invitation...'
            : 'Create invitation link'}
        </button>
      </form>

      {state.status === 'error' && (
        <div
          role="alert"
          className="mt-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-950"
        >
          <p>{state.message}</p>

          {state.reference && (
            <p className="mt-2 break-all font-mono text-xs">
              Reference: {state.reference}
            </p>
          )}
        </div>
      )}

      {state.status === 'created' && link && (
        <div
          role="status"
          className="mt-5 space-y-3 rounded-xl border border-lime-300 bg-lime-50 p-4"
        >
          <p className="font-semibold text-lime-950">
            Invitation created
          </p>

          <label
            htmlFor="created-invite-link"
            className="block text-sm font-semibold"
          >
            Invitation link
          </label>

          <input
            id="created-invite-link"
            type="text"
            readOnly
            value={link}
            onFocus={event =>
              event.currentTarget.select()
            }
            className="min-h-11 w-full rounded-lg border border-lime-300 bg-white px-3 py-2 text-sm"
          />

          <button
            type="button"
            onClick={() => void copyLink()}
            className="min-h-11 rounded-lg border border-lime-700 px-4 py-2 font-semibold text-lime-900"
          >
            Copy full invitation link
          </button>

          <p className="text-sm text-lime-900">
            {copied
              ? 'Link copied.'
              : 'You can also select and copy the link above.'}
          </p>
        </div>
      )}
    </section>
  )
}
