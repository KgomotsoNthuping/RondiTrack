import {
  createLoader,
  createParser,
} from 'nuqs/server'

import { isValidCalendarDate } from './southAfricanDates'

export const HISTORY_PAGE_SIZE = 5

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

const memberParser = createParser({
  parse(value: string) {
    return uuidPattern.test(value)
      ? value.toLowerCase()
      : null
  },
  serialize(value: string) {
    return value
  },
})

const calendarDateParser = createParser({
  parse(value: string) {
    return isValidCalendarDate(value)
      ? value
      : null
  },
  serialize(value: string) {
    return value
  },
})

export const historyFilters = {
  member: memberParser,
  from: calendarDateParser,
  to: calendarDateParser,
}

export const loadHistoryFilters = createLoader(
  historyFilters,
)
