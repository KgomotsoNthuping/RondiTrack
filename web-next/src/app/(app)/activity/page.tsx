import { Suspense } from 'react'
import { Activity } from 'lucide-react'

import Loading from '@/app/(app)/loading'
import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { ActivityCard } from '@/components/ActivityCard'
import { ActivityFeed } from '@/components/ActivityFeed'
import { loadMemberActivityPage } from '@/lib/activity-data'

async function ActivityContent() {
  const page = await loadMemberActivityPage()

  if (page instanceof ApiError) {
    return <ApiErrorPanel error={page} />
  }

  return (
    <div className="space-y-7">
      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Member activity
        </p>

        <h1 className="mt-2 text-3xl font-extrabold">
          Activity feed
        </h1>

        <p className="mt-2 text-slate-600">
          Contributions, payouts and members joining
          across your stokvels, newest first.
        </p>
      </header>

      {page.items.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <Activity
            aria-hidden="true"
            className="mx-auto size-9 text-slate-400"
          />

          <h2 className="mt-4 text-xl font-bold">
            No activity yet
          </h2>

          <p className="mt-2 text-slate-600">
            Activity will appear here when events
            are recorded in your stokvels.
          </p>
        </section>
      ) : (
        <ActivityFeed
          key={`${page.nextPageToken}|${page.items[0].id}`}
          after={page.nextPageToken}
          firstItemId={page.items[0].id}
          firstCount={page.items.length}
        >
          {page.items.map(item => (
            <ActivityCard
              key={`${item.type}:${item.stokvelId}:${item.id}`}
              item={item}
            />
          ))}
        </ActivityFeed>
      )}
    </div>
  )
}

export default function ActivityPage() {
  return (
    <Suspense fallback={<Loading />}>
      <ActivityContent />
    </Suspense>
  )
}
