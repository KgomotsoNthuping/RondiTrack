const dateFormatter = new Intl.DateTimeFormat('en-ZA', {
  dateStyle: 'medium',
  timeZone: 'Africa/Johannesburg',
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-ZA', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'Africa/Johannesburg',
})

export function formatDate(value: string): string {
  return dateFormatter.format(new Date(value))
}

export function formatDateTime(value: string): string {
  return dateTimeFormatter.format(new Date(value))
}

export function formatCalendarDate(value: string): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    throw new RangeError('Expected YYYY-MM-DD.')
  }

  const date = new Date(`${value}T12:00:00Z`)

  if (
    Number.isNaN(date.getTime()) ||
    date.toISOString().slice(0, 10) !== value
  ) {
    throw new RangeError('Invalid calendar date.')
  }

  return dateFormatter.format(date)
}
