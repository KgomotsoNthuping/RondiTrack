import { createStore } from 'zustand/vanilla'
import {
  createJSONStorage,
  persist,
} from 'zustand/middleware'

import {
  normalisePins,
  type PinnedStokvel,
} from '@/lib/pinnedStokvels'

export type PinnedStokvelsState = {
  pins: PinnedStokvel[]
  hydrated: boolean
  pin: (value: PinnedStokvel) => void
  unpin: (id: string) => void
  clearPins: () => void
}

export function createPinnedStokvelsStore(
  storageKey: string,
) {
  return createStore<PinnedStokvelsState>()(
    persist(
      set => ({
        pins: [],
        hydrated: false,

        pin: value =>
          set(state => ({
            pins: normalisePins([
              ...state.pins,
              value,
            ]),
          })),

        unpin: id =>
          set(state => ({
            pins: state.pins.filter(
              pin => pin.id !== id,
            ),
          })),

        clearPins: () =>
          set({
            pins: [],
          }),
      }),
      {
        name: storageKey,
        storage: createJSONStorage(
          () => localStorage,
        ),
        skipHydration: true,
        partialize: state => ({
          pins: state.pins,
        }),
        merge: (persisted, current) => {
          const saved =
            persisted !== null &&
            typeof persisted === 'object' &&
            'pins' in persisted
              ? persisted.pins
              : []

          return {
            ...current,
            pins: normalisePins(saved),
          }
        },
      },
    ),
  )
}

export type PinnedStokvelsStore = ReturnType<
  typeof createPinnedStokvelsStore
>
