export function contributionStatusLabel(
  status: string | null | undefined,
): string {
  switch (status) {
    case 'Pending':
      return 'Pending'

    case 'Paid':
      return 'Paid'

    case 'Confirmed':
      return 'Confirmed'

    default:
      return 'Unknown status'
  }
}
