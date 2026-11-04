import { z } from 'zod'

export const invitePreviewSchema = z.object({
  stokvelId: z.string().uuid(),
  name: z.string(),
  rules: z.string().nullable(),
  contributionAmountInCents: z.number().int().nonnegative(),
  currentPeriod: z.number().int(),
  totalPeriods: z.number().int(),
  nextContributionDueDate: z.string().nullable(),
  memberCount: z.number().int().nonnegative(),
  alreadyMember: z.boolean(),
})

export const joinResponseSchema = z.object({
  stokvelId: z.string().uuid(),
  name: z.string(),
  role: z.string(),
})

export const createInviteResponseSchema = z.object({
  code: z.string(),
  expiresAtUtc: z.string(),
})

export type InvitePreview = z.infer<
  typeof invitePreviewSchema
>
