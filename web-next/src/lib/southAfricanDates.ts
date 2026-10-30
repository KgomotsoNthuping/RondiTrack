export type SouthAfricanUtcRange = {
  fromUtc: string | null
  toUtc: string | null
  isReversed: boolean
}

const SAST_OFFSET_MS = 2 * 60 * 60 * 1000
const DAY_MS = 24 * 60 * 60 * 1000

export function isValidCalendarDate(
  value: string,
): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false
  }

  const year = Number(value.slice(0, 4))

  if (year < 1000) {
    return false
  }

  const date = new Date(`${value}T00:00:00.000Z`)

  return (
    Number.isFinite(date.getTime()) &&
    date.toISOString().slice(0, 10) === value
  )
}

function toUtcString(milliseconds: number): string {
  return new Date(milliseconds)
    .toISOString()
    .replace('.000Z', 'Z')
}

export function southAfricanDaysToUtcRange(
  from: string | null,
  to: string | null,
): SouthAfricanUtcRange {
  if (from !== null && !isValidCalendarDate(from)) {
    throw new RangeError('Invalid from date.')
  }

  if (to !== null && !isValidCalendarDate(to)) {
    throw new RangeError('Invalid to date.')
  }

  if (from !== null && to !== null && to < from) {
    return {
      fromUtc: null,
      toUtc: null,
      isReversed: true,
    }
  }

  const fromUtc =
    from === null
      ? null
      : toUtcString(
          Date.parse(`${from}T00:00:00.000Z`) -
            SAST_OFFSET_MS,
        )

  const toUtc =
    to === null
      ? null
      : toUtcString(
          Date.parse(`${to}T00:00:00.000Z`) +
            DAY_MS -
            SAST_OFFSET_MS,
        )

  return {
    fromUtc,
    toUtc,
    isReversed: false,
  }
}
