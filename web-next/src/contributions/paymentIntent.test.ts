
import { describe, expect, it } from 'vitest'

import {
  PaymentIntentTracker,
  type PaymentDetails,
} from './paymentIntent'

const initialKey =
  '123e4567-e89b-42d3-a456-426614174001'

const payment: PaymentDetails = {
  cycleId: '123e4567-e89b-42d3-a456-426614174002',
  userId: '123e4567-e89b-42d3-a456-426614174003',
  amountInCents: 15050,
}

describe('PaymentIntentTracker', () => {
  it('uses the key generated during server rendering', () => {
    const tracker = new PaymentIntentTracker(initialKey)

    expect(tracker.keyFor(payment)).toBe(initialKey)
  })

  it('reuses the key for an unchanged retry', () => {
    const tracker = new PaymentIntentTracker(initialKey)

    const first = tracker.keyFor(payment)
    const retry = tracker.keyFor(payment)

    expect(retry).toBe(first)
  })

  it('generates a new key when payment details change', () => {
    const secondKey = crypto.randomUUID()

    const tracker = new PaymentIntentTracker(
      initialKey,
      () => secondKey,
    )

    const first = tracker.keyFor(payment)

    const changed = tracker.keyFor({
      ...payment,
      amountInCents: 16050,
    })

    expect(changed).toBe(secondKey)
    expect(changed).not.toBe(first)
  })

  it('starts a new payment after success', () => {
    const nextKey = crypto.randomUUID()
    const tracker = new PaymentIntentTracker(initialKey)

    tracker.keyFor(payment)
    tracker.reset(nextKey)

    expect(tracker.keyFor(payment)).toBe(nextKey)
    expect(nextKey).not.toBe(initialKey)
  })
})
