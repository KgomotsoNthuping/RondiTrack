import { describe, expect, it } from 'vitest'
import { loadHistoryFilters } from './historyFilters'

const memberId =
  '123e4567-e89b-42d3-a456-426614174000'

describe('history URL filters', () => {
  it('defaults to all contributions', async () => {
    expect(
      await loadHistoryFilters(''),
    ).toEqual({
      member: null,
      from: null,
      to: null,
    })
  })

  it('accepts a valid member and October dates', async () => {
    expect(
      await loadHistoryFilters(
        `?member=${memberId}&from=2026-10-01&to=2026-10-31`,
      ),
    ).toEqual({
      member: memberId,
      from: '2026-10-01',
      to: '2026-10-31',
    })
  })

  it('ignores an invalid member ID', async () => {
    const result = await loadHistoryFilters(
      '?member=banana',
    )

    expect(result.member).toBeNull()
  })

  it('ignores a nonexistent calendar date', async () => {
    const result = await loadHistoryFilters(
      '?from=2026-02-30&to=yesterday',
    )

    expect(result.from).toBeNull()
    expect(result.to).toBeNull()
  })

  it('preserves a valid single-day selection', async () => {
    const result = await loadHistoryFilters(
      '?from=2026-10-31&to=2026-10-31',
    )

    expect(result.from).toBe('2026-10-31')
    expect(result.to).toBe('2026-10-31')
  })
})
