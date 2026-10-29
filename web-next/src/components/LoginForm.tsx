
'use client'

import { useActionState } from 'react'
import Link from 'next/link'
import { LockKeyhole, Wallet } from 'lucide-react'

import {
  loginAction,
  type LoginState,
} from '@/actions/auth'

const initialState: LoginState = {
  message: null,
  reference: null,
  fieldErrors: {},
}

type LoginFormProps = {
  expired?: boolean
  registered?: boolean
  next?: string | null
}

export function LoginForm({
  expired = false,
  registered = false,
  next = null,
}: LoginFormProps) {
  const [state, formAction, pending] = useActionState(
    loginAction,
    initialState,
  )

  const registerHref = next
    ? `/register?${new URLSearchParams({ next }).toString()}`
    : '/register'

  const inputClass =
    'min-h-11 w-full rounded-lg border border-slate-300 ' +
    'bg-white px-3 py-2 text-slate-950 outline-none ' +
    'focus-visible:ring-2 focus-visible:ring-lime-500'

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-8">
      <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-lg sm:p-8">
        <header className="mb-7 flex items-center gap-3">
          <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-lime-400 text-lime-950">
            <Wallet
              aria-hidden="true"
              className="size-6"
            />
          </div>

          <div>
            <h1 className="text-2xl font-extrabold text-slate-950">
              RondiTrack
            </h1>

            <p className="text-sm text-slate-600">
              Sign in to manage your stokvels
            </p>
          </div>
        </header>

        {registered && (
          <div
            role="status"
            className="mb-5 rounded-lg border border-lime-200 bg-lime-50 p-3 text-sm text-lime-950"
          >
            Your account has been created successfully.
            Sign in to continue.
          </div>
        )}

        {expired && (
          <div
            role="status"
            className="mb-5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950"
          >
            Your session has ended. Please sign in again.
          </div>
        )}

        {state.message && (
          <div
            role="alert"
            className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-950"
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
          className="space-y-5"
        >
          {next && (
            <input
              type="hidden"
              name="next"
              value={next}
            />
          )}

          <div className="space-y-2">
            <label
              htmlFor="login-email"
              className="block text-sm font-semibold text-slate-900"
            >
              Email address
            </label>

            <input
              id="login-email"
              name="email"
              type="email"
              autoComplete="username"
              required
              placeholder="you@example.com"
              aria-invalid={Boolean(
                state.fieldErrors.email?.length,
              )}
              className={inputClass}
            />

            {state.fieldErrors.email?.map(
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
          </div>

          <div className="space-y-2">
            <label
              htmlFor="login-password"
              className="block text-sm font-semibold text-slate-900"
            >
              Password
            </label>

            <input
              id="login-password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              aria-invalid={Boolean(
                state.fieldErrors.password?.length,
              )}
              className={inputClass}
            />

            {state.fieldErrors.password?.map(
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
          </div>

          <button
            type="submit"
            disabled={pending}
            className="flex min-h-11 w-full items-center justify-center gap-2 rounded-lg bg-lime-400 px-4 py-3 font-bold text-lime-950 hover:bg-lime-300 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <LockKeyhole
              aria-hidden="true"
              className="size-4"
            />

            {pending ? 'Signing in...' : 'Sign in'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-600">
          Don't have an account?{' '}
          <Link
            href={registerHref}
            className="font-semibold text-lime-800 hover:underline"
          >
            Create an account
          </Link>
        </p>
      </section>
    </main>
  )
}
