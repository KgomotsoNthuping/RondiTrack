type FlightEntry<T> = {
  promise: Promise<T>
  settledAt: number | null
}

export function createSingleFlight<T>(
  ttlMs: number,
  now: () => number = Date.now,
) {
  const entries = new Map<string, FlightEntry<T>>()

  return function runOnce(
    key: string,
    task: () => Promise<T>,
  ): Promise<T> {
    const currentTime = now()

    // Remove expired completed entries.
    for (const [storedKey, entry] of entries) {
      if (
        entry.settledAt !== null &&
        currentTime - entry.settledAt >= ttlMs
      ) {
        entries.delete(storedKey)
      }
    }

    const existing = entries.get(key)

    if (existing) {
      return existing.promise
    }

    /*
     * Defer task execution to the next microtask.
     * The shared Promise is registered first.
     */
    const promise = Promise.resolve().then(task)

    const entry: FlightEntry<T> = {
      promise,
      settledAt: null,
    }

    entries.set(key, entry)

    void promise.then(
      () => {
        entry.settledAt = now()
      },
      () => {
        if (entries.get(key) === entry) {
          entries.delete(key)
        }
      },
    )

    return promise
  }
}
