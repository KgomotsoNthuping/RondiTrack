'use client'

import { LogOut } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'

import { logoutAction } from '@/actions/auth'
import { clearAllSavedPins } from '@/lib/pinnedStokvels'
import { usePinnedStokvelsStore } from './PinnedStokvelsProvider'

export function LogoutForm() {
  const store = usePinnedStokvelsStore()
  const queryClient = useQueryClient()

  function prepareLogout() {
    store.getState().clearPins()
    void store.persist.clearStorage()
    clearAllSavedPins()
    queryClient.clear()
  }

  return (
    <form
      action={logoutAction}
      onSubmit={prepareLogout}
      className="mt-5"
    >
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
  )
}