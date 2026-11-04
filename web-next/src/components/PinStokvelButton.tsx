'use client'

import { Pin, PinOff } from 'lucide-react'
import { usePinnedStokvels } from './PinnedStokvelsProvider'

type Props = {
  stokvelId: string
  stokvelName: string
}

export function PinStokvelButton({
  stokvelId,
  stokvelName,
}: Props) {
  const hydrated = usePinnedStokvels(
    state => state.hydrated,
  )

  const isPinned = usePinnedStokvels(
    state =>
      state.pins.some(
        pin => pin.id === stokvelId,
      ),
  )

  const pin = usePinnedStokvels(
    state => state.pin,
  )

  const unpin = usePinnedStokvels(
    state => state.unpin,
  )

  function togglePin() {
    if (!hydrated) {
      return
    }

    if (isPinned) {
      unpin(stokvelId)
    } else {
      pin({
        id: stokvelId,
        name: stokvelName,
      })
    }
  }

  const Icon = isPinned ? PinOff : Pin

  return (
    <button
      type="button"
      onClick={togglePin}
      disabled={!hydrated}
      aria-pressed={isPinned}
      aria-label={
        isPinned
          ? `Unpin ${stokvelName}`
          : `Pin ${stokvelName}`
      }
      className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-800 hover:border-lime-600 hover:bg-lime-50 disabled:cursor-wait disabled:opacity-60"
    >
      <Icon
        aria-hidden="true"
        className="size-4"
      />

      {isPinned ? 'Unpin' : 'Pin'}
    </button>
  )
}
