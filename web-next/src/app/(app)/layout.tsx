
import type { ReactNode } from 'react'
import { Suspense } from 'react'
import Link from 'next/link'
import { redirect } from 'next/navigation'
import {
  Activity,
  House,
  LayoutDashboard,
  Users,
  Wallet,
} from 'lucide-react'

import { getSession } from '@/lib/session'
import { LogoutForm } from '@/components/LogoutForm'
import { PinnedStokvelsNav } from '@/components/PinnedStokvelsNav'
import { PinnedStokvelsProvider } from '@/components/PinnedStokvelsProvider'
import { Providers } from './Providers'
import type { Metadata } from 'next'


export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
    nocache: true,
  },
}


const navigation = [
  {
    href: '/',
    label: 'Home',
    icon: House,
  },
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
  {
    href: '/activity',
    label: 'Activity feed',
    icon: Activity,
  },
  {
  href: '/join',
  label: 'Join a stokvel',
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
    <PinnedStokvelsProvider
      key={session.userId}
      userId={session.userId}
    >
      <div className="min-h-screen bg-slate-50 text-slate-950">      
        <a
          href="#main-content"
          className="fixed left-4 top-4 z-[100] -translate-y-32 rounded-lg bg-lime-400 px-5 py-3 font-bold text-lime-950 shadow-lg focus:translate-y-0 focus-visible:outline focus-visible:outline-4 focus-visible:outline-offset-2 focus-visible:outline-slate-950"
        >
          Skip to main content
        </a>

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

            <PinnedStokvelsNav />

            <div className="mt-8 border-t border-slate-200 pt-5">
              <p className="break-words font-semibold">
                {session.fullName}
              </p>

              <p className="mt-1 break-all text-sm text-slate-500">
                {session.email}
              </p>

              <LogoutForm />
            </div>
          </aside>

          
          <main
            id="main-content"
            tabIndex={-1}
            className="min-w-0 flex-1 p-5 sm:p-8"
          >

            {children}
          </main>
        </div>
      </div>
    </PinnedStokvelsProvider>
  )
}

export default function AppLayout({
  children,
}: {
  children: ReactNode
}) {
  return (
    <Providers>
      <Suspense
        
        fallback={
          <div
            role="status"
            aria-label="Loading RondiTrack"
            className="mx-auto flex min-h-screen max-w-7xl flex-col md:flex-row"
          >
            <aside
              aria-hidden="true"
              className="w-full shrink-0 border-b border-slate-200 bg-white p-5 md:min-h-screen md:w-64 md:border-b-0 md:border-r"
            >
              <div className="h-11 w-40 rounded-xl bg-slate-200" />
              <div className="mt-8 grid grid-cols-2 gap-2 md:grid-cols-1">
                {Array.from({ length: 6 }, (_, index) => (
                  <div
                    key={index}
                    className="h-11 rounded-xl bg-slate-100"
                  />
                ))}
              </div>
              <div className="mt-8 h-24 border-t border-slate-200 pt-5">
                <div className="h-5 w-32 rounded bg-slate-200" />
                <div className="mt-3 h-4 w-40 rounded bg-slate-100" />
              </div>
            </aside>
            <main className="min-w-0 flex-1 p-5 sm:p-8">
              <div className="animate-pulse space-y-6">
                <div className="h-4 w-32 rounded bg-slate-200" />
                <div className="h-9 w-64 max-w-full rounded bg-slate-200" />
                <div className="h-4 w-full max-w-lg rounded bg-slate-200" />
                <div className="grid gap-4 sm:grid-cols-3">
                  {Array.from({ length: 3 }, (_, index) => (
                    <div
                      key={index}
                      className="h-32 rounded-2xl border border-slate-200 bg-white"
                    />
                  ))}
                </div>
              </div>
            </main>
            <span className="sr-only">
              Loading RondiTrack information...
            </span>
          </div>
}

      >
        <AppShell>{children}</AppShell>
      </Suspense>
    </Providers>
  )
}
