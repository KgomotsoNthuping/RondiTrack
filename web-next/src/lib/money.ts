const zar = new Intl.NumberFormat('en-ZA', {
  style: 'currency',
  currency: 'ZAR',
})

export function formatCents(cents: number): string {
  if (!Number.isSafeInteger(cents)) {
    throw new RangeError(
      `Expected a whole number of cents, got ${cents}`,
    )
  }

  const sign = cents < 0 ? '-' : ''
  const absolute = Math.abs(cents)

  const remainder = absolute % 100
  const rands = (absolute - remainder) / 100

  const decimal =
    `${sign}${rands}.${String(remainder).padStart(2, '0')}`

  return zar.format(decimal as `${number}`)
}

export function parseRandsToCents(
  input: string,
): number | null {
  const value = input.trim()

  const moneyPattern =
    /^(?:\d+|\d{1,3}(?: \d{3})+)(?:[.,]\d{1,2})?$/

  if (!moneyPattern.test(value)) {
    return null
  }

  const [wholePart, fractionPart = ''] =
    value.split(/[.,]/)

  const wholeDigits = wholePart.replaceAll(' ', '')

  const centsDigits =
    wholeDigits + fractionPart.padEnd(2, '0')

  const cents = BigInt(centsDigits)

  if (cents > BigInt(Number.MAX_SAFE_INTEGER)) {
    return null
  }

  return Number(cents)
}

export function centsToInput(cents: number): string {
  if (!Number.isSafeInteger(cents) || cents < 0) {
    throw new RangeError(
      'Cents must be a non-negative safe integer.',
    )
  }

  const remainder = cents % 100
  const rands = (cents - remainder) / 100

  return `${rands}.${String(remainder).padStart(2, '0')}`
}
