import { z } from 'zod'
import { parseRandsToCents } from '../lib/money'

export const contributionSchema = z.object({
  userId: z.string().uuid('Select a valid member.'),
  amount: z
    .string()
    .trim()
    .min(1, 'Enter a contribution amount.')
    .refine(
      value => {
        const cents = parseRandsToCents(value)
        return cents !== null && cents > 0
      },
      'Enter a valid amount greater than zero, with no more than two decimal places.',
    ),
})

export type ContributionFormValues = z.infer<
  typeof contributionSchema
>

export type ContributionRequestValues = {
  userId: string
  amountInCents: number
}

export function toContributionRequest(
  values: ContributionFormValues,
): ContributionRequestValues {
  const cents = parseRandsToCents(values.amount)

  if (cents === null || cents <= 0) {
    throw new RangeError('Invalid contribution amount.')
  }

  return {
    userId: values.userId,
    amountInCents: cents,
  }
}