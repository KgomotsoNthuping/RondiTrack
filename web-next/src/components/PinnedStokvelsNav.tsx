'use client'

import Link from 'next/link'
import { Pin } from 'lucide-react'
import { usePinnedStokvels } from './PinnedStokvelsProvider'

export function PinnedStokvelsNav() {
  const hydrated = usePinnedStokvels(
    state => state.hydrated,
  )

  const pins = usePinnedStokvels(
    state => state.pins,
  )

  return (
    <section
      aria-labelledby="pinned-stokvels-title"
      className="mt-6 border-t border-slate-200 pt-5"
    >
      <h2
        id="pinned-stokvels-title"
        className="flex items-center gap-2 text-sm font-bold text-slate-900"
      >
        <Pin
          aria-hidden="true"
          className="size-4 text-lime-700"
        />
        Pinned stokvels
      </h2>

      {!hydrated ? (
        <p
          role="status"
          className="mt-3 text-sm text-slate-500"
        >
          Restoring pins...
        </p>
      ) : pins.length === 0 ? (
        <p className="mt-3 text-sm text-slate-500">
          No pinned stokvels yet.
        </p>
      ) : (
        <ul className="mt-3 space-y-1">
          {pins.map(pin => (
            <li key={pin.id}>
              <Link
                href={`/stokvels/${pin.id}`}
                className="block min-h-11 break-words rounded-lg px-3 py-3 text-sm font-semibold text-slate-700 hover:bg-lime-100 hover:text-lime-950"
              >
                {pin.name}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
