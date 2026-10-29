'use client'

import { useActionState } from 'react'
import Link from 'next/link'
import { UserPlus, Wallet } from 'lucide-react'

import {
  registerAction,
  type RegistrationState,
} from '@/actions/register'

const initialState: RegistrationState = {
  message: null,
  reference: null,
  fieldErrors: {},
}

type RegisterFormProps = {
  next?: string | null
}

export function RegisterForm({
  next = null,
}: RegisterFormProps) {
  const [state, formAction, pending] = useActionState(
    registerAction,
    initialState,
  )

  const loginHref = next
    ? `/login?${new URLSearchParams({ next }).toString()}`
    : '/login'

  const inputClass =
    'min-h-11 w-full rounded-lg border border-slate-300 ' +
    'bg-white px-3 py-2 text-slate-950 outline-none ' +
    'focus-visible:ring-2 focus-visible:ring-lime-500'

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-8">
      <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-lg sm:p-8">
        <header className="mb-7 flex items-center gap-3">
          <div className="flex size-12 items-center justify-center rounded-xl bg-lime-400 text-lime-950">
            <Wallet
              aria-hidden="true"
              className="size-6"
            />
          </div>

          <div>
            <h1 className="text-2xl font-extrabold text-slate-950">
              Create your account
            </h1>

            <p className="text-sm text-slate-600">
              Join RondiTrack
            </p>
          </div>
        </header>

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

        <form action={formAction} className="space-y-5">
          {next && (
            <input
              type="hidden"
              name="next"
              value={next}
            />
          )}

          <div className="space-y-2">
            <label
              htmlFor="register-name"
              className="block text-sm font-semibold"
            >
              Full name
            </label>

            <input
              id="register-name"
              name="fullName"
              type="text"
              autoComplete="name"
              required
              aria-invalid={
                Boolean(state.fieldErrors.fullName?.length)
              }
              className={inputClass}
            />

            {state.fieldErrors.fullName?.map(
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
              htmlFor="register-email"
              className="block text-sm font-semibold"
            >
              Email address
            </label>

            <input
              id="register-email"
              name="email"
              type="email"
              autoComplete="username"
              required
              className={inputClass}
              aria-invalid={
                Boolean(state.fieldErrors.email?.length)
              }
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
              htmlFor="register-phone"
              className="block text-sm font-semibold"
            >
              Phone number
            </label>

            <input
              id="register-phone"
              name="phoneNumber"
              type="tel"
              autoComplete="tel"
              required
              className={inputClass}
              aria-invalid={
                Boolean(state.fieldErrors.phoneNumber?.length)
              }
            />

            {state.fieldErrors.phoneNumber?.map(
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
              htmlFor="register-password"
              className="block text-sm font-semibold"
            >
              Password
            </label>

            <input
              id="register-password"
              name="password"
              type="password"
              autoComplete="new-password"
              minLength={12}
              required
              className={inputClass}
              aria-invalid={
                Boolean(state.fieldErrors.password?.length)
              }
            />

            <p className="text-xs text-slate-500">
              Use at least 12 characters.
            </p>

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

          <div className="space-y-2">
            <label
              htmlFor="register-confirm-password"
              className="block text-sm font-semibold"
            >
              Confirm password
            </label>

            <input
              id="register-confirm-password"
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              required
              className={inputClass}
              aria-invalid={Boolean(
                state.fieldErrors.confirmPassword?.length,
              )}
            />

            {state.fieldErrors.confirmPassword?.map(
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
            <UserPlus
              aria-hidden="true"
              className="size-4"
            />

            {pending
              ? 'Creating account...'
              : 'Create account'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-600">
          Already have an account?{' '}
          <Link
            href={loginHref}
            className="font-semibold text-lime-800 hover:underline"
          >
            Sign in
          </Link>
        </p>
      </section>
    </main>
  )
}
