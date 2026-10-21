
import { useState, type FormEvent } from 'react'
import { LockKeyhole, Wallet } from 'lucide-react'

import { login } from '../auth/session'
import { describeError } from '../lib/errorMessages'
import { ThemeSwitcher } from './ThemeSwitcher'

type LoginFormProps = {
  onSuccess: () => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (loading) return

    setLoading(true)
    setError(null)

    try {
      await login(email.trim(), password)

      setPassword('')
      onSuccess()
    } catch (cause) {
      setError(cause)
    } finally {
      setLoading(false)
    }
  }

  const description =
    error !== null ? describeError(error) : null

  // Login must not reveal whether the email or
  // password was incorrect.
  const loginMessage =
    description?.title === 'Session ended'
      ? 'Email or password is incorrect.'
      : description?.message

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4 py-8 text-foreground">
      <section className="w-full max-w-md rounded-2xl border border-border bg-card p-6 text-card-foreground shadow-lg sm:p-8">
        <div className="mb-6 flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-lime-400 text-lime-950">
              <Wallet aria-hidden="true" className="size-6" />
            </div>

            <div>
              <h1 className="text-2xl font-extrabold">
                RondiTrack
              </h1>

              <p className="text-sm text-muted-foreground">
                Sign in to manage your stokvels
              </p>
            </div>
          </div>

          <ThemeSwitcher />
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label
              htmlFor="login-email"
              className="block text-sm font-semibold"
            >
              Email address
            </label>

            <input
              id="login-email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="min-h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
              placeholder="you@example.com"
            />

            {description?.fieldErrors.email?.map((message, index) => (
              <p key={index} className="text-sm text-red-700 dark:text-red-300">
                {message}
              </p>
            ))}
          </div>

          <div className="space-y-2">
            <label
              htmlFor="login-password"
              className="block text-sm font-semibold"
            >
              Password
            </label>

            <input
              id="login-password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="min-h-11 w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />

            {description?.fieldErrors.password?.map((message, index) => (
              <p key={index} className="text-sm text-red-700 dark:text-red-300">
                {message}
              </p>
            ))}
          </div>

          {loginMessage && (
            <div
              role="alert"
              className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-950 dark:border-red-800 dark:bg-red-950 dark:text-red-100"
            >
              <p>{loginMessage}</p>

              {description?.reference && (
                <p className="mt-2 break-all font-mono text-xs">
                  Reference: {description.reference}
                </p>
              )}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="flex min-h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-3 font-bold text-primary-foreground hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <LockKeyhole aria-hidden="true" className="size-4" />
            {loading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  )
}
