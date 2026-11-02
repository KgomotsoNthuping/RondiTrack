'use client'

import {
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'

import { HistoryContributionCard } from '@/components/HistoryContributionCard'
import { fetchExtraPage } from '@/lib/feedFetch'
import {
  historyFeedKey,
  type HistoryKeyFilters,
} from '@/lib/feedKeys'
import type { ContributionHistoryPage } from '@/lib/historyTypes'

type HistoryFeedProps = {
  stokvelId: string
  filters: HistoryKeyFilters
  after: string
  firstItemId: string
  firstCount: number
  children: ReactNode
}

export function HistoryFeed({
  stokvelId,
  filters,
  after,
  firstItemId,
  firstCount,
  children,
}: HistoryFeedProps) {
  const [wanted, setWanted] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const more = useInfiniteQuery({
    queryKey: historyFeedKey(
      stokvelId,
      filters,
      after,
      firstItemId,
    ),
    queryFn: async ({
      pageParam,
    }): Promise<ContributionHistoryPage> => {
      const query = new URLSearchParams({
        pageToken: pageParam,
      })

      if (filters.member) {
        query.set('member', filters.member)
      }

      if (filters.from) {
        query.set('from', filters.from)
      }

      if (filters.to) {
        query.set('to', filters.to)
      }

      return fetchExtraPage<ContributionHistoryPage>(
        `/bff/stokvels/${encodeURIComponent(stokvelId)}/contributions?${query.toString()}`,
      )
    },
    initialPageParam: after,
    getNextPageParam: page =>
      page.nextPageToken || undefined,
    enabled: wanted,
  })

  const extraCount =
    more.data?.pages.reduce(
      (total, page) => total + page.items.length,
      0,
    ) ?? 0

  const totalCount = firstCount + extraCount

  const canLoadMore =
    after.length > 0 &&
    (
      !wanted ||
      more.isPending ||
      more.hasNextPage ||
      more.isError
    )

  useEffect(() => {
    const element = sentinelRef.current

    if (
      !element ||
      !canLoadMore ||
      more.isFetching ||
      more.isError
    ) {
      return
    }

    const observer = new IntersectionObserver(
      entries => {
        if (!entries[0]?.isIntersecting) {
          return
        }

        if (!wanted) {
          setWanted(true)
        } else if (more.hasNextPage) {
          void more.fetchNextPage()
        }
      },
      {
        rootMargin: '180px',
      },
    )

    observer.observe(element)

    return () => observer.disconnect()
  }, [
    canLoadMore,
    wanted,
    more.isFetching,
    more.isError,
    more.hasNextPage,
    more.fetchNextPage,
  ])

  function loadMore() {
    if (!wanted) {
      setWanted(true)
      return
    }

    if (more.isError) {
      void more.refetch()
      return
    }

    if (
      more.hasNextPage &&
      !more.isFetching
    ) {
      void more.fetchNextPage()
    }
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 lg:grid-cols-2">
        {children}

        {more.data?.pages.flatMap(page =>
          page.items.map(item => (
            <HistoryContributionCard
              key={item.id}
              item={item}
            />
          )),
        )}
      </div>

      <p
        role="status"
        aria-live="polite"
        className="text-sm text-slate-600"
      >
        Showing {totalCount}{' '}
        {totalCount === 1
          ? 'contribution'
          : 'contributions'}.
        {' '}
        {canLoadMore
          ? 'More records are available.'
          : 'All available contributions are shown.'}
      </p>

      {more.isError && (
        <p
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          {more.error.message}
        </p>
      )}

      {canLoadMore && (
        <div className="flex flex-col items-center gap-3">
          <div
            ref={sentinelRef}
            aria-hidden="true"
            className="h-1 w-full"
          />

          <button
            type="button"
            onClick={loadMore}
            disabled={more.isFetching}
            className="min-h-11 rounded-xl border border-lime-700 bg-white px-6 py-3 font-semibold text-lime-800 hover:bg-lime-50 disabled:cursor-wait disabled:opacity-60"
          >
            {more.isFetching
              ? 'Loading more...'
              : more.isError
                ? 'Retry loading'
                : 'Load more'}
          </button>
        </div>
      )}
    </div>
  )
}
