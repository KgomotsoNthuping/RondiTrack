import { Suspense } from 'react'
import Link from 'next/link'
import { redirect } from 'next/navigation'

import { JoinHeading } from '@/components/JoinHeading'
import { JoinProgress } from '@/components/JoinProgress'
import { InviteSummary } from '@/components/InviteSummary'
import { JoinConfirmForm } from '@/components/JoinConfirmForm'
import { getInvitePreview } from '@/lib/invites'
import {
  joinHref,
  parseJoinStep,
  transitionJoin,
  type JoinState,
} from '@/lib/joinFlow'

type JoinPageProps = {
  searchParams: Promise<{
    code?: string | string[]
    step?: string | string[]
  }>
}

async function JoinContent({
  searchParams,
}: JoinPageProps) {
  const params = await searchParams

  const code =
    typeof params.code === 'string'
      ? params.code.trim().slice(0, 256)
      : ''

  const requestedStep = parseJoinStep(params.step)

  if (requestedStep === null) {
    redirect(joinHref(code, 'code'))
  }

  if (requestedStep !== 'code' && !code) {
    redirect('/join')
  }

  const lookup = code
    ? await getInvitePreview(code, requestedStep)
    : null

  if (
    requestedStep !== 'code' &&
    (!lookup ||
      lookup.kind !== 'ready' ||
      lookup.preview.alreadyMember)
  ) {
    redirect(joinHref(code, 'code'))
  }

  let state: JoinState = {
    step: 'code',
    code,
  }

  if (
    requestedStep !== 'code' &&
    lookup?.kind === 'ready'
  ) {
    const accepted = transitionJoin(
      state,
      {
        type: 'CODE_ACCEPTED',
        preview: lookup.preview,
      },
    )

    if (accepted.step !== 'rules') {
      redirect(joinHref(code, 'code'))
    }

    state =
      requestedStep === 'confirm'
        ? transitionJoin(
            accepted,
            { type: 'CONTINUE' },
          )
        : accepted
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <header className="space-y-2">
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Join a stokvel
        </p>

        <JoinHeading
          key={state.step}
          step={state.step}
        />
      </header>

      <JoinProgress step={state.step} />

      <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        {state.step === 'code' ? (
          <div className="space-y-5">
            <p className="text-slate-700">
              Enter the invitation code shared by
              a Treasurer of the stokvel.
            </p>

            <form
              action="/join"
              method="get"
              className="space-y-4"
            >
              <input
                type="hidden"
                name="step"
                value="rules"
              />

              <div className="space-y-2">
                <label
                  htmlFor="invite-code"
                  className="block text-sm font-semibold"
                >
                  Invitation code
                </label>

                <input
                  id="invite-code"
                  name="code"
                  type="text"
                  defaultValue={code}
                  autoComplete="off"
                  required
                  maxLength={256}
                  className="min-h-11 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-lime-500"
                  placeholder="Paste invitation code"
                />
              </div>

              <button
                type="submit"
                className="min-h-11 w-full rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300"
              >
                Check invitation and continue
              </button>
            </form>

            {lookup?.kind === 'unknown' && (
              <div
                role="alert"
                className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"
              >
                Check the invitation code and try again.
              </div>
            )}

            {lookup?.kind === 'expired' && (
              <div
                role="alert"
                className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"
              >
                This invitation has expired or was revoked.
                Ask the Treasurer for a new invitation.
              </div>
            )}

            {lookup?.kind === 'rate-limited' && (
              <div
                role="alert"
                className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"
              >
                Too many attempts. Try again later.
                {lookup.retryAfter && (
                  <p className="mt-2">
                    Retry-After: {lookup.retryAfter}
                  </p>
                )}
              </div>
            )}

            {lookup?.kind === 'unavailable' && (
              <div
                role="alert"
                className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-950"
              >
                <p>
                  The invitation service is temporarily
                  unavailable. Please try again.
                </p>

                {lookup.reference && (
                  <p className="mt-2 break-all font-mono text-xs">
                    Reference: {lookup.reference}
                  </p>
                )}
              </div>
            )}

            {lookup?.kind === 'ready' && (
              lookup.preview.alreadyMember ? (
                <div
                  role="status"
                  className="rounded-xl border border-lime-300 bg-lime-50 p-4"
                >
                  <p className="font-semibold">
                    You are already a member of{' '}
                    {lookup.preview.name}.
                  </p>

                  <Link
                    href={`/stokvels/${lookup.preview.stokvelId}`}
                    className="mt-3 inline-flex min-h-11 items-center rounded-lg bg-lime-400 px-4 py-2 font-bold text-lime-950"
                  >
                    Open stokvel
                  </Link>
                </div>
              ) : (
                <div
                  role="status"
                  className="rounded-xl border border-lime-300 bg-lime-50 p-4"
                >
                  <p className="font-semibold">
                    Valid invitation for {lookup.preview.name}
                  </p>

                  <Link
                    href={joinHref(code, 'rules')}
                    className="mt-3 inline-flex min-h-11 items-center rounded-lg bg-lime-400 px-4 py-2 font-bold text-lime-950"
                  >
                    Continue to rules
                  </Link>
                </div>
              )
            )}
          </div>
        ) : state.step === 'rules' ? (
          <div className="space-y-6">
            <InviteSummary preview={state.preview} />

            <div className="flex flex-wrap gap-3 border-t border-slate-200 pt-5">
              <Link
                href={joinHref(
                  state.code,
                  transitionJoin(
                    state,
                    { type: 'BACK' },
                  ).step,
                )}
                className="inline-flex min-h-11 items-center rounded-xl border border-slate-300 px-5 py-3 font-semibold"
              >
                Back to code
              </Link>

              <Link
                href={joinHref(
                  state.code,
                  transitionJoin(
                    state,
                    { type: 'CONTINUE' },
                  ).step,
                )}
                className="inline-flex min-h-11 items-center rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300"
              >
                Continue to confirmation
              </Link>
            </div>
          </div>
        ) : (
          <div className="space-y-6">
            <InviteSummary preview={state.preview} />

            <Link
              href={joinHref(
                state.code,
                transitionJoin(
                  state,
                  { type: 'BACK' },
                ).step,
              )}
              className="inline-flex min-h-11 items-center rounded-xl border border-slate-300 px-5 py-3 font-semibold"
            >
              Back to rules
            </Link>

            <JoinConfirmForm
              code={state.code}
              stokvelName={state.preview.name}
            />
          </div>
        )}
      </section>
    </div>
  )
}

export default function JoinPage({
  searchParams,
}: JoinPageProps) {
  return (
    <Suspense
      fallback={
        <div
          role="status"
          className="mx-auto max-w-3xl rounded-2xl border border-slate-200 bg-white p-8"
        >
          Loading your invitation...
        </div>
      }
    >
      <JoinContent searchParams={searchParams} />
    </Suspense>
  )
}
