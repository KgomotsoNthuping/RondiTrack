export type PaymentDetails = {
  cycleId: string
  userId: string
  amountInCents: number
}

export class PaymentIntentTracker {
  private signature: string | null = null
  private key: string | null = null

  keyFor(details: PaymentDetails): string {
    const signature = JSON.stringify([
      details.cycleId,
      details.userId,
      details.amountInCents,
    ])

    if (this.signature !== signature || this.key === null) {
      this.signature = signature
      this.key = crypto.randomUUID()
    }

    return this.key
  }

  clear(): void {
    this.signature = null
    this.key = null
  }
}
