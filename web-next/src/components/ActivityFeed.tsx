'use client'

import {
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'

import { ActivityCard } from '@/components/ActivityCard'
import { fetchExtraPage } from '@/lib/feedFetch'
import { activityFeedKey } from '@/lib/feedKeys'
import type { MemberActivityPage } from '@/lib/activityTypes'

type ActivityFeedProps = {
  after: string
  firstItemId: string
  firstCount: number
  children: ReactNode
}

export function ActivityFeed({
  after,
  firstItemId,
  firstCount,
  children,
}: ActivityFeedProps) {
  const [wanted, setWanted] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const more = useInfiniteQuery({
    queryKey: activityFeedKey(after, firstItemId),
    queryFn: async ({
      pageParam,
    }): Promise<MemberActivityPage> => {
      const query = new URLSearchParams({
        pageToken: pageParam,
      })

      return fetchExtraPage<MemberActivityPage>(
        `/bff/activity?${query.toString()}`,
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
    <section className="space-y-5">
      <div className="grid gap-4 lg:grid-cols-2">
        {children}

        {more.data?.pages.flatMap(page =>
          page.items.map(item => (
            <ActivityCard
              key={`${item.type}:${item.stokvelId}:${item.id}`}
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
          ? 'activity event'
          : 'activity events'}.
        {' '}
        {canLoadMore
          ? 'More activity is available.'
          : 'All available activity is shown.'}
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
    </section>
  )
}
