const saDate = new Intl.DateTimeFormat('en-ZA', {
  dateStyle: 'medium',
  timeZone: 'Africa/Johannesburg',
})

const saDateTime = new Intl.DateTimeFormat('en-ZA', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'Africa/Johannesburg',
})

// Format a UTC timestamp as a South African date.
export function formatDate(
  isoInstant: string,
): string {
  return saDate.format(new Date(isoInstant))
}

// Format a UTC timestamp with South African time.
export function formatDateTime(
  isoInstant: string,
): string {
  return saDateTime.format(new Date(isoInstant))
}

// Format a calendar date such as "2026-10-31".
// This is a date, not a UTC payment timestamp.
export function formatCalendarDate(
  dateOnly: string,
): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(dateOnly)) {
    throw new RangeError('Expected YYYY-MM-DD')
  }

  const date = new Date(`${dateOnly}T12:00:00Z`)

  if (Number.isNaN(date.getTime()) ||
      date.toISOString().slice(0, 10) !== dateOnly) {
    throw new RangeError('Invalid calendar date')
  }

  return saDate.format(date)
}
