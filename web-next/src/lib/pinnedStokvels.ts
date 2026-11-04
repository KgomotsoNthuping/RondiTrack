export type PinnedStokvel = {
  id: string
  name: string
}

export const PIN_STORAGE_PREFIX = 'ronditrack:pins:v1:'

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function pinsStorageKey(userId: string): string {
  return `${PIN_STORAGE_PREFIX}${userId.toLowerCase()}`
}

export function normalisePins(
  value: unknown,
): PinnedStokvel[] {
  if (!Array.isArray(value)) {
    return []
  }

  const pins: PinnedStokvel[] = []

  for (const entry of value) {
    if (
      entry === null ||
      typeof entry !== 'object' ||
      Array.isArray(entry)
    ) {
      continue
    }

    const candidate = entry as Record<string, unknown>

    if (
      typeof candidate.id !== 'string' ||
      typeof candidate.name !== 'string'
    ) {
      continue
    }

    const id = candidate.id.toLowerCase()
    const name = candidate.name.trim()

    if (
      !uuidPattern.test(id) ||
      name.length === 0 ||
      name.length > 120
    ) {
      continue
    }

    const existingIndex = pins.findIndex(
      pin => pin.id === id,
    )

    if (existingIndex !== -1) {
      pins.splice(existingIndex, 1)
    }

    pins.push({ id, name })
  }

  return pins.slice(-20)
}

export function clearAllSavedPins(): void {
  if (typeof window === 'undefined') {
    return
  }

  try {
    const storage = window.localStorage
    const keys: string[] = []

    for (
      let index = 0;
      index < storage.length;
      index++
    ) {
      const key = storage.key(index)

      if (key?.startsWith(PIN_STORAGE_PREFIX)) {
        keys.push(key)
      }
    }

    for (const key of keys) {
      storage.removeItem(key)
    }
  } catch {
    return
  }
}