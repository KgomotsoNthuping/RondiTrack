
import { describe, expect, it } from 'vitest'
import { settleForStream } from './settleForStream'

describe('settleForStream', () => {
  it('preserves a successful response', async () => {
    const result = await settleForStream(
      Promise.resolve(['one', 'two']),
    )

    expect(result).toEqual({
      ok: true,
      value: ['one', 'two'],
    })
  })

  it('captures a rejected API request', async () => {
    const error = new Error('API unavailable')

    const result = await settleForStream(
      Promise.reject(error),
    )

    expect(result).toEqual({
      ok: false,
      error,
    })
  })

  it('does not turn an empty result into an error', async () => {
    const result = await settleForStream(
      Promise.resolve([]),
    )

    expect(result).toEqual({
      ok: true,
      value: [],
    })
  })
})
