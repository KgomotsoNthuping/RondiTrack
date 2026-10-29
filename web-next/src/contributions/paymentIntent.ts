export type PaymentDetails = {
  cycleId: string
  userId: string
  amountInCents: number
}

export class PaymentIntentTracker {
  private signature: string | null = null
  private key: string

  constructor(
    initialKey: string,
    private readonly generateKey: () => string =
      () => crypto.randomUUID(),
  ) {
    this.key = initialKey
  }

  keyFor(details: PaymentDetails): string {
    const signature = JSON.stringify([
      details.cycleId,
      details.userId,
      details.amountInCents,
    ])

    // An unchanged payment must keep its key.
    // A changed payment must start a new attempt.
    if (
      this.signature !== null &&
      this.signature !== signature
    ) {
      this.key = this.generateKey()
    }

    this.signature = signature

    return this.key
  }

  reset(nextServerKey: string): void {
    this.signature = null
    this.key = nextServerKey
  }
}
