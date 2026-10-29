import { Suspense } from 'react'
import { LoginForm } from '@/components/LoginForm'
import { safeNext } from '@/lib/safeNext'

type LoginPageProps = {
  searchParams: Promise<{
    expired?: string
    registered?: string
    next?: string
  }>
}

async function LoginFormFromUrl({
  searchParams,
}: LoginPageProps) {
  const params = await searchParams

  return (
    <LoginForm
      expired={params.expired === '1'}
      registered={params.registered === '1'}
      next={safeNext(params.next)}
    />
  )
}

export default function LoginPage({
  searchParams,
}: LoginPageProps) {
  return (
    <Suspense
      fallback={
        <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
          <div
            role="status"
            className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center text-slate-600 shadow-sm"
          >
            Loading sign-in...
          </div>
        </main>
      }
    >
      <LoginFormFromUrl
        searchParams={searchParams}
      />
    </Suspense>
  )
}
