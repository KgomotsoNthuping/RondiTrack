import { useCallback, useEffect, useState } from 'react'
import { LogOut, Wallet } from 'lucide-react'
import { ThemeProvider } from './components/ThemeProvider'
import { ThemeSwitcher } from './components/ThemeSwitcher'
import { StokvelCard } from './components/StokvelCard'
import { ContributionList } from './components/ContributionList'
import { DueCountdown } from './components/DueCountdown'
import { ArrearsBadge } from './components/StatusBadge'
import { LoginForm } from './components/LoginForm'
import { ErrorView } from './components/ErrorView'
import { RecordContributionForm } from './components/RecordContributionForm'
import {
  useMe,
  useStokvels,
  useCycles,
  useContributions,
} from './api/queries'
import { useContributionMembers } from './api/members'
import { useRecordContribution } from './api/recordContribution'
import { queryClient } from './api/queryClient'
import { ApiError } from './api/ApiError'
import { logout, onSignedOut } from './auth/session'
import './styles/theme-overrides.css'

function LoadingView({
  message,
}: {
  message: string
}) {
  return (
    <div
      role="status"
      className="animate-pulse rounded-xl border border-border bg-card p-6 text-card-foreground"
    >
      <div className="mb-4 h-5 w-36 rounded bg-muted" />
      <div className="mb-3 h-4 w-full rounded bg-muted" />
      <div className="mb-3 h-4 w-3/4 rounded bg-muted" />
      <span className="sr-only">{message}</span>
    </div>
  )
}

function Dashboard() {
  const me = useMe()
  const stokvelsQuery = useStokvels()
  const [selectedId, setSelectedId] = useState<string | null>(
    null,
  )

  const stokvels = stokvelsQuery.data ?? []

  const activeId =
    selectedId &&
    stokvels.some(
      stokvel => stokvel.stokvelId === selectedId,
    )
      ? selectedId
      : (stokvels[0]?.stokvelId ?? null)

  const selectedStokvel =
    stokvels.find(
      stokvel => stokvel.stokvelId === activeId,
    ) ?? null

  const cyclesQuery = useCycles(activeId)
  const contributionsQuery = useContributions(activeId)

  const isTreasurer =
    selectedStokvel?.role === 'Treasurer'

  const membersQuery = useContributionMembers(
    activeId,
    isTreasurer,
  )

  const recordContribution = useRecordContribution()

  const contributions =
    contributionsQuery.data?.pages.flatMap(
      page => page.items,
    ) ?? []

  const activeCycle =
    cyclesQuery.data?.find(
      cycle =>
        cycle.status === 'Open' &&
        cycle.cycleNumber === selectedStokvel?.currentCycle,
    ) ??
    cyclesQuery.data?.find(
      cycle => cycle.status === 'Open',
    ) ??
    null

  const activeCycleId = activeCycle?.id ?? null

  const contributionMembers = isTreasurer
    ? (membersQuery.data ?? [])
    : me.data
      ? [
          {
            id: me.data.id,
            fullName: me.data.fullName,
          },
        ]
      : []

  if (me.isPending || stokvelsQuery.isPending) {
    return (
      <main className="min-h-screen bg-background p-6">
        <LoadingView
          message="Loading your RondiTrack account"
        />
      </main>
    )
  }

  if (me.isError) {
    return (
      <main className="min-h-screen bg-background p-6">
        <ErrorView
          error={me.error}
          onRetry={() => void me.refetch()}
        />
      </main>
    )
  }

  if (stokvelsQuery.isError) {
    return (
      <main className="min-h-screen bg-background p-6">
        <ErrorView
          error={stokvelsQuery.error}
          onRetry={() => void stokvelsQuery.refetch()}
        />
      </main>
    )
  }

  return (
    <div className="min-h-screen min-w-0 bg-background text-foreground">
      <header className="w-full bg-lime-400 text-lime-950 shadow-sm">
        <div className="mx-auto flex min-h-20 max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-lime-950 text-lime-300">
              <Wallet
                aria-hidden="true"
                className="size-6"
              />
            </span>

            <div className="min-w-0">
              <h1 className="text-xl font-extrabold sm:text-2xl">
                RondiTrack
              </h1>
              <p className="text-xs font-medium">
                Your stokvels, all in one place
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <ThemeSwitcher />

            <button
              type="button"
              onClick={() => {
                void logout()
              }}
              className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-lime-950 px-3 py-2 text-sm font-semibold text-lime-100 hover:bg-lime-900"
            >
              <LogOut
                aria-hidden="true"
                className="size-4"
              />
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl space-y-10 px-4 py-8 sm:px-6">
        <section
          aria-labelledby="stokvels-heading"
          className="space-y-5"
        >
          <div>
            <h2
              id="stokvels-heading"
              className="text-xl font-bold text-foreground"
            >
              My Stokvels
            </h2>

            <p className="mt-1 text-sm text-muted-foreground">
              Signed in as {me.data?.fullName ?? 'Member'}
            </p>
          </div>

          {stokvels.length === 0 ? (
            <div className="rounded-xl border border-border bg-card p-6 text-card-foreground">
              <p className="font-semibold">
                You're not part of a stokvel yet.
              </p>
              <p className="mt-2 text-sm text-muted-foreground">
                Ask a stokvel administrator to add you as a member.
              </p>
            </div>
          ) : (
            <div className="grid min-w-0 gap-5 md:grid-cols-2">
              {stokvels.map(stokvel => (
                <div
                  key={stokvel.stokvelId}
                  className="min-w-0 space-y-3 rounded-2xl border border-border bg-card p-3 text-card-foreground shadow-sm sm:p-4"
                >
                  <StokvelCard stokvel={stokvel} />

                  <div className="flex flex-wrap items-center gap-3 border-t border-border pt-3">
                    <DueCountdown
                      dueDate={stokvel.cycleDueDate}
                      status={stokvel.cycleStatus}
                    />
                    <ArrearsBadge
                      amountInCents={stokvel.arrearsInCents}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        {stokvels.length > 0 && (
          <section
            aria-labelledby="contributions-heading"
            className="space-y-5"
          >
            <div className="flex flex-wrap items-end justify-between gap-4">
              <div>
                <h2
                  id="contributions-heading"
                  className="text-xl font-bold text-foreground"
                >
                  Contributions by cycle
                </h2>

                <p className="mt-1 text-sm text-muted-foreground">
                  Select a stokvel to view its contribution history.
                </p>
              </div>

              <div className="min-w-0 space-y-2">
                <label
                  htmlFor="stokvel-switcher"
                  className="block text-sm font-semibold"
                >
                  Select stokvel
                </label>

                <select
                  id="stokvel-switcher"
                  value={activeId ?? ''}
                  onChange={event => {
                    setSelectedId(event.target.value)
                  }}
                  className="min-h-11 w-full max-w-full rounded-lg border border-input bg-card px-3 py-2 text-card-foreground sm:min-w-64"
                >
                  {stokvels.map(stokvel => (
                    <option
                      key={stokvel.stokvelId}
                      value={stokvel.stokvelId}
                    >
                      {stokvel.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <p className="break-words text-sm font-semibold text-foreground">
              Viewing: {selectedStokvel?.name}
            </p>

            {cyclesQuery.isPending ||
            contributionsQuery.isPending ? (
              <LoadingView
                message="Loading this stokvel's contributions"
              />
            ) : cyclesQuery.isError ? (
              <ErrorView
                error={cyclesQuery.error}
                onRetry={() => void cyclesQuery.refetch()}
                onBack={() => {
                  setSelectedId(null)
                  document
                    .getElementById('stokvels-heading')
                    ?.scrollIntoView({
                      behavior: 'smooth',
                    })
                }}
              />
            ) : contributionsQuery.isError ? (
              <ErrorView
                error={contributionsQuery.error}
                onRetry={() =>
                  void contributionsQuery.refetch()
                }
                onBack={() => {
                  setSelectedId(null)
                  document
                    .getElementById('stokvels-heading')
                    ?.scrollIntoView({
                      behavior: 'smooth',
                    })
                }}
              />
            ) : cyclesQuery.data.length === 0 ? (
              <div className="rounded-xl border border-border bg-card p-6 text-card-foreground">
                <p className="font-semibold">
                  No contribution cycles yet.
                </p>
                <p className="mt-2 text-sm text-muted-foreground">
                  A treasurer can create a cycle before members contribute.
                </p>
              </div>
            ) : (
              <>
                {selectedStokvel?.cycleStatus !== 'Open' ||
                !activeCycle ? (
                  <div className="rounded-xl border border-border bg-card p-5 text-card-foreground">
                    <h3 className="text-lg font-semibold">
                      Record contribution
                    </h3>
                    <p className="mt-2 text-sm text-muted-foreground">
                      Contributions can only be recorded
                      during an open cycle.
                    </p>
                  </div>
                ) : isTreasurer &&
                  membersQuery.isPending ? (
                  <LoadingView
                    message="Loading stokvel members"
                  />
                ) : isTreasurer &&
                  membersQuery.isError ? (
                  <ErrorView
                    error={membersQuery.error}
                    onRetry={() =>
                      void membersQuery.refetch()
                    }
                  />
                ) : me.data && activeCycleId ? (
                  <RecordContributionForm
                    key={`${activeId}:${activeCycleId}`}
                    currentUserId={me.data.id}
                    currentUserName={me.data.fullName}
                    memberRole={selectedStokvel.role}
                    members={contributionMembers}
                    cycleId={activeCycleId}
                    cycleNumber={activeCycle.cycleNumber}
                    cycleStatus={activeCycle.status}
                    amountOwedInCents={
                      selectedStokvel.stillOwedInCents ??
                      0
                    }
                    onRecord={async values => {
                      if (!activeId || !activeCycleId) {
                        throw new Error(
                          'An open cycle is required.',
                        )
                      }

                      await recordContribution.mutateAsync({
                        stokvelId: activeId,
                        cycleId: activeCycleId,
                        ...values,
                      })
                    }}
                  />
                ) : null}

                <ContributionList
                  key={activeId}
                  cycles={cyclesQuery.data}
                  contributions={contributions}
                />

                {contributionsQuery.hasNextPage && (
                  <button
                    type="button"
                    disabled={
                      contributionsQuery.isFetchingNextPage
                    }
                    onClick={() => {
                      void contributionsQuery.fetchNextPage()
                    }}
                    className="min-h-11 rounded-lg bg-primary px-5 py-3 font-semibold text-primary-foreground disabled:opacity-60"
                  >
                    {contributionsQuery.isFetchingNextPage
                      ? 'Loading more...'
                      : 'Load more contributions'}
                  </button>
                )}
              </>
            )}
          </section>
        )}
      </main>
    </div>
  )
}

function AuthenticatedApp({
  onSessionExpired,
}: {
  onSessionExpired: () => void
}) {
  const me = useMe()

  useEffect(() => {
    if (
      me.isError &&
      me.error instanceof ApiError &&
      me.error.status === 401
    ) {
      onSessionExpired()
    }
  }, [me.isError, me.error, onSessionExpired])

  if (me.isPending) {
    return (
      <main className="min-h-screen bg-background p-6">
        <LoadingView message="Restoring your session" />
      </main>
    )
  }

  if (
    me.isError &&
    me.error instanceof ApiError &&
    me.error.status === 401
  ) {
    return null
  }

  if (me.isError) {
    return (
      <main className="min-h-screen bg-background p-6">
        <ErrorView
          error={me.error}
          onRetry={() => void me.refetch()}
        />
      </main>
    )
  }

  return <Dashboard />
}

export default function App() {
  const [needsLogin, setNeedsLogin] = useState(false)
  const [sessionKey, setSessionKey] = useState(0)

  useEffect(() => {
    return onSignedOut(() => {
      void queryClient.cancelQueries()
      queryClient.clear()
      setNeedsLogin(true)
    })
  }, [])

  const handleSessionExpired = useCallback(() => {
    void queryClient.cancelQueries()
    queryClient.clear()
    setNeedsLogin(true)
  }, [])

  function handleLoginSuccess() {
    queryClient.clear()
    setSessionKey(value => value + 1)
    setNeedsLogin(false)
  }

  return (
    <ThemeProvider>
      {needsLogin ? (
        <LoginForm onSuccess={handleLoginSuccess} />
      ) : (
        <AuthenticatedApp
          key={sessionKey}
          onSessionExpired={handleSessionExpired}
        />
      )}
    </ThemeProvider>
  )
}
