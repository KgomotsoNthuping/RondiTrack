// South Africa is UTC+02:00 year-round (no daylight saving time).
const SA_OFFSET_MS = 2 * 60 * 60 * 1000
const DAY_MS = 24 * 60 * 60 * 1000

function parseDateOnly(value: string): [number, number, number] {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) throw new Error('Expected YYYY-MM-DD')
  const [year, month, day] = value.split('-').map(Number)
  const date = new Date(Date.UTC(year, month - 1, day))
  if (date.getUTCFullYear() !== year || date.getUTCMonth() !== month - 1 || date.getUTCDate() !== day)
    throw new Error('Invalid calendar date')
  return [year, month, day]
}

// Date-only deadline: midnight *after* the due day in Johannesburg.
export function saDueDateDeadline(value: string): Date {
  const [year, month, day] = parseDateOnly(value)
  return new Date(Date.UTC(year, month - 1, day + 1) - SA_OFFSET_MS)
}

function saDayIndex(date: Date): number {
  return Math.floor((date.getTime() + SA_OFFSET_MS) / DAY_MS)
}

export function dueCountdownLabel(dueDate: string, now: Date): string {
  const due = saDueDateDeadline(dueDate)
  const daysUntil = saDayIndex(due) - saDayIndex(now) - 1
  if (daysUntil === 0) return 'Due today'
  if (daysUntil === 1) return 'Due tomorrow'
  if (daysUntil > 1) return `Due in ${daysUntil} days`
  return `Overdue by ${Math.abs(daysUntil)} day${daysUntil === -1 ? '' : 's'}`
}

export function millisecondsUntilNextSaMidnight(now: Date): number {
  const timestamp = now.getTime()
  return (Math.floor((timestamp + SA_OFFSET_MS) / DAY_MS) + 1) * DAY_MS - SA_OFFSET_MS - timestamp
}
