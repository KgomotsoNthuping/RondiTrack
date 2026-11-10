'use client'

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { useStore } from 'zustand'

import { pinsStorageKey } from '@/lib/pinnedStokvels'
import {
  createPinnedStokvelsStore,
  type PinnedStokvelsState,
  type PinnedStokvelsStore,
} from '@/stores/pinnedStokvelsStore'

const PinnedStokvelsContext =
  createContext<PinnedStokvelsStore | null>(null)

type Props = {
  userId: string
  children: ReactNode
}

export function PinnedStokvelsProvider({
  userId,
  children,
}: Props) {
  const storageKey = pinsStorageKey(userId)

  const [store] = useState(() =>
    createPinnedStokvelsStore(storageKey),
  )

  useEffect(() => {
    let active = true

    const rehydrate = store.persist.rehydrate()

    if (rehydrate && typeof rehydrate.then === 'function') {
      void rehydrate.then(
        () => {
          if (active) {
            store.setState({ hydrated: true })
          }
        },
        () => {
          if (active) {
            store.setState({ hydrated: true })
          }
        },
      )
    } else if (active) {
      store.setState({ hydrated: true })
    }

    function onStorage(event: StorageEvent) {
      if (
        event.key === storageKey &&
        event.newValue === null
      ) {
        store.getState().clearPins()
      }
    }

    window.addEventListener('storage', onStorage)

    return () => {
      active = false
      window.removeEventListener(
        'storage',
        onStorage,
      )
    }
  }, [store, storageKey])

  return (
    <PinnedStokvelsContext.Provider value={store}>
      {children}
    </PinnedStokvelsContext.Provider>
  )
}

export function usePinnedStokvels<T>(
  selector: (state: PinnedStokvelsState) => T,
): T {
  const store = useContext(PinnedStokvelsContext)

  if (!store) {
    throw new Error(
      'PinnedStokvelsProvider is missing.',
    )
  }

  return useStore(store, selector)
}

export function usePinnedStokvelsStore():
  PinnedStokvelsStore {
  const store = useContext(PinnedStokvelsContext)

  if (!store) {
    throw new Error(
      'PinnedStokvelsProvider is missing.',
    )
  }

  return store
}
