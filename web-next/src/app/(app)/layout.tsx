import type { ReactNode } from 'react'
import { Suspense } from 'react'
import Link from 'next/link'
import { redirect } from 'next/navigation'
import {
  House,
  LayoutDashboard,
  Users,
  Wallet,
  LogOut,
} from 'lucide-react'

import { logoutAction } from '@/actions/auth'
import { getSession } from '@/lib/session'

const navigation = [
  { href: '/', label: 'Home', icon: House },
  {
    href: '/dashboard/member',
    label: 'Member dashboard',
    icon: LayoutDashboard,
  },
  {
    href: '/dashboard/treasurer',
    label: 'Treasurer dashboard',
    icon: Wallet,
  },
  {
    href: '/stokvels',
    label: 'My stokvels',
    icon: Users,
  },
]

async function AppShell({
  children,
}: {
  children: ReactNode
}) {
  const session = await getSession()

  if (!session) {
    redirect('/login')
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <div className="mx-auto flex min-h-screen max-w-7xl flex-col md:flex-row">
        <aside className="w-full border-b border-slate-200 bg-white p-5 md:min-h-screen md:w-64 md:shrink-0 md:border-b-0 md:border-r">
          <Link
            href="/dashboard/member"
            className="flex items-center gap-3"
          >
            <span className="flex size-11 items-center justify-center rounded-xl bg-lime-400 font-extrabold text-lime-950">
              R
            </span>
            <span className="text-xl font-extrabold">
              RondiTrack
            </span>
          </Link>

          <nav
            aria-label="Main navigation"
            className="mt-8 flex flex-wrap gap-2 md:flex-col"
          >
            {navigation.map(item => {
              const Icon = item.icon

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className="flex min-h-11 items-center gap-3 rounded-xl px-4 py-3 text-sm font-semibold text-slate-700 transition-colors hover:bg-lime-100 hover:text-lime-950"
                >
                  <Icon
                    aria-hidden="true"
                    className="size-5"
                  />
                  {item.label}
                </Link>
              )
            })}
          </nav>

          <div className="mt-8 border-t border-slate-200 pt-5">
            <p className="break-words font-semibold">
              {session.fullName}
            </p>
            <p className="mt-1 break-all text-sm text-slate-500">
              {session.email}
            </p>

            <form action={logoutAction} className="mt-5">
              <button
                type="submit"
                className="flex min-h-11 w-full items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold hover:bg-slate-100"
              >
                <LogOut
                  aria-hidden="true"
                  className="size-4"
                />
                Sign out
              </button>
            </form>
          </div>
        </aside>

        <main
          id="main-content"
          className="min-w-0 flex-1 p-5 sm:p-8"
        >
          {children}
        </main>
      </div>
    </div>
  )
}

export default function AppLayout({
  children,
}: {
  children: ReactNode
}) {
  return (
    <Suspense
      fallback={
        <div
          role="status"
          className="p-8 text-slate-600"
        >
          Loading RondiTrack...
        </div>
      }
    >
      <AppShell>{children}</AppShell>
    </Suspense>
  )
}
