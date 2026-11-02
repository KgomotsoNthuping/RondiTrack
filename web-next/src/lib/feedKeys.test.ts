import { describe, expect, it } from 'vitest'
import {
  activityFeedKey,
  historyFeedKey,
} from './feedKeys'

const initialFilters = {
  member: null,
  from: '2026-10-01',
  to: '2026-10-31',
}

describe('feed query keys', () => {
  it('preserves the key for unchanged history filters', () => {
    expect(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    ).toEqual(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    )
  })

  it('changes the key when the member changes', () => {
    expect(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    ).not.toEqual(
      historyFeedKey(
        'stokvel-a',
        {
          ...initialFilters,
          member: 'member-b',
        },
        'cursor-a',
        'first-a',
      ),
    )
  })

  it('changes the key when the date range changes', () => {
    expect(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    ).not.toEqual(
      historyFeedKey(
        'stokvel-a',
        {
          ...initialFilters,
          to: '2026-11-30',
        },
        'cursor-a',
        'first-a',
      ),
    )
  })

  it('changes the key when the first page changes', () => {
    expect(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    ).not.toEqual(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-b',
        'first-b',
      ),
    )
  })

  it('separates different stokvel histories', () => {
    expect(
      historyFeedKey(
        'stokvel-a',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    ).not.toEqual(
      historyFeedKey(
        'stokvel-b',
        initialFilters,
        'cursor-a',
        'first-a',
      ),
    )
  })

  it('changes the activity key when page one changes', () => {
    expect(
      activityFeedKey('cursor-a', 'first-a'),
    ).not.toEqual(
      activityFeedKey('cursor-b', 'first-b'),
    )
  })
})
