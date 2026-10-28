'use client'

import { useRouter } from 'next/navigation'
import { RefreshCw } from 'lucide-react'

export function RetryButton() {
  const router = useRouter()

  return (
    <button
      type="button"
      onClick={() => router.refresh()}
      className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-red-900 px-5 py-3 font-semibold text-white hover:bg-red-800"
    >
      <RefreshCw
        aria-hidden="true"
        className="size-4"
      />
      Try again
    </button>
  )
}
