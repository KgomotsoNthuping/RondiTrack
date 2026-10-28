
export type StreamResult<T> =
  | {
      ok: true
      value: T
    }
  | {
      ok: false
      error: unknown
    }

export function settleForStream<T>(
  task: Promise<T>,
): Promise<StreamResult<T>> {
  return task.then(
    value => ({
      ok: true as const,
      value,
    }),
    error => ({
      ok: false as const,
      error,
    }),
  )
}
