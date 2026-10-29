import { Suspense } from 'react'
import { RegisterForm } from '@/components/RegisterForm'
import { safeNext } from '@/lib/safeNext'

type RegisterPageProps = {
  searchParams: Promise<{
    next?: string
  }>
}

async function RegistrationFromUrl({
  searchParams,
}: RegisterPageProps) {
  const params = await searchParams

  return (
    <RegisterForm
      next={safeNext(params.next)}
    />
  )
}

export default function RegisterPage({
  searchParams,
}: RegisterPageProps) {
  return (
    <Suspense
      fallback={
        <main className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
          <div
            role="status"
            className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center text-slate-600"
          >
            Loading registration...
          </div>
        </main>
      }
    >
      <RegistrationFromUrl
        searchParams={searchParams}
      />
    </Suspense>
  )
}
