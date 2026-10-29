import {
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { createSingleFlight } from './singleFlight'

describe('single-flight refresh coordination', () => {
  it('shares one task across three concurrent calls', async () => {
    const once = createSingleFlight<string>(
      10_000,
    )

    const task = vi.fn(
      async () => 'refreshed-session',
    )

    const results = await Promise.all([
      once('same-token', task),
      once('same-token', task),
      once('same-token', task),
    ])

    expect(results).toEqual([
      'refreshed-session',
      'refreshed-session',
      'refreshed-session',
    ])

    expect(task).toHaveBeenCalledTimes(1)
  })

  it('reuses a completed result inside the grace period', async () => {
    let clock = 0

    const once = createSingleFlight<string>(
      10_000,
      () => clock,
    )

    const task = vi.fn(
      async () => 'new-session',
    )

    const first = await once('token-a', task)

    clock = 5_000

    const second = await once('token-a', task)

    expect(first).toBe('new-session')
    expect(second).toBe('new-session')
    expect(task).toHaveBeenCalledTimes(1)
  })

  it('starts a new task after the grace period', async () => {
    let clock = 0

    const once = createSingleFlight<number>(
      10_000,
      () => clock,
    )

    let calls = 0

    const task = async () => ++calls

    const first = await once('token-a', task)

    clock = 10_001

    const second = await once('token-a', task)

    expect(first).toBe(1)
    expect(second).toBe(2)
    expect(calls).toBe(2)
  })

  it('allows another attempt after a rejected task', async () => {
    const once = createSingleFlight<string>(
      10_000,
    )

    const failure = vi.fn(async () => {
      throw new Error('Temporary failure')
    })

    await expect(
      once('token-a', failure),
    ).rejects.toThrow('Temporary failure')

    const success = vi.fn(
      async () => 'recovered',
    )

    await expect(
      once('token-a', success),
    ).resolves.toBe('recovered')

    expect(failure).toHaveBeenCalledTimes(1)
    expect(success).toHaveBeenCalledTimes(1)
  })
})
