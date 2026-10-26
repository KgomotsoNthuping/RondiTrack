import { useRef, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { centsToInput, formatCents } from '../lib/money'
import {
  contributionSchema,
  toContributionRequest,
  type ContributionFormValues,
  type ContributionRequestValues,
} from '../forms/contribution'

export type RecordableMember = {
  id: string
  fullName: string
}

export type RecordContributionFormProps = {
  currentUserId: string
  currentUserName: string
  memberRole: string
  members: RecordableMember[]
  cycleNumber: number
  cycleStatus: string
  amountOwedInCents: number
  onRecord: (values: ContributionRequestValues) => Promise<void>
}

export function RecordContributionForm({
  currentUserId,
  currentUserName,
  memberRole,
  members,
  cycleNumber,
  cycleStatus,
  amountOwedInCents,
  onRecord,
}: RecordContributionFormProps) {
  const submittingRef = useRef(false)
  const [confirmation, setConfirmation] = useState<string | null>(null)
  const [submissionError, setSubmissionError] = useState<string | null>(null)

  const isTreasurer = memberRole === 'Treasurer'
  const isOpen = cycleStatus === 'Open'

  const {
    register,
    handleSubmit,
    setValue,
    setFocus,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<ContributionFormValues>({
    resolver: zodResolver(contributionSchema),
    defaultValues: {
      userId: currentUserId,
      amount:
        amountOwedInCents > 0
          ? centsToInput(amountOwedInCents)
          : '',
    },
  })

  if (!isOpen) {
    return (
      <section className="rounded-xl border border-border bg-card p-5 text-card-foreground">
        <h2 className="text-lg font-semibold">
          Record contribution
        </h2>
        <p className="mt-2 text-sm text-muted-foreground">
          This cycle is {cycleStatus.toLowerCase()}.
          Contributions can only be recorded for an open cycle.
        </p>
      </section>
    )
  }

  async function submit(values: ContributionFormValues) {
    if (submittingRef.current) {
      return
    }

    submittingRef.current = true
    setConfirmation(null)
    setSubmissionError(null)

    try {
      const request = toContributionRequest(values)

      const memberName =
        members.find(member => member.id === request.userId)
          ?.fullName ?? currentUserName

      await onRecord(request)

      setConfirmation(
        `${formatCents(request.amountInCents)} recorded for ${memberName} in cycle ${cycleNumber}.`,
      )

      setValue('amount', '', {
        shouldDirty: false,
        shouldValidate: false,
      })

      setValue('userId', currentUserId, {
        shouldDirty: false,
        shouldValidate: false,
      })

      clearErrors()
      setFocus('amount')
    } catch {
      setSubmissionError(
        'The contribution could not be confirmed. It may have been recorded. You can retry safely once the connection is available.',
      )
    } finally {
      submittingRef.current = false
    }
  }

  return (
    <section className="rounded-xl border border-border bg-card p-5 text-card-foreground shadow-sm">
      <div className="mb-5">
        <h2 className="text-lg font-semibold">
          Record contribution
        </h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Cycle {cycleNumber}
        </p>
        <p className="mt-2 text-sm">
          Amount outstanding:{' '}
          <strong>{formatCents(amountOwedInCents)}</strong>
        </p>
      </div>

      <form
        noValidate
        onSubmit={handleSubmit(submit)}
        className="space-y-5"
      >
        <div className="space-y-2">
          <label
            htmlFor="contribution-user"
            className="block text-sm font-medium"
          >
            Member
          </label>

          {isTreasurer ? (
            <select
              id="contribution-user"
              {...register('userId')}
              aria-invalid={Boolean(errors.userId)}
              aria-describedby={
                errors.userId ? 'contribution-user-error' : undefined
              }
              disabled={isSubmitting}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground"
            >
              <option value="">Select member</option>
              {members.map(member => (
                <option key={member.id} value={member.id}>
                  {member.fullName}
                </option>
              ))}
            </select>
          ) : (
            <>
              <p
                id="contribution-user"
                className="rounded-lg border border-input bg-background px-3 py-2 text-sm"
              >
                {currentUserName}
              </p>
              <input
                type="hidden"
                {...register('userId')}
              />
            </>
          )}

          {errors.userId && (
            <p
              id="contribution-user-error"
              className="text-sm text-destructive"
              role="alert"
            >
              {errors.userId.message}
            </p>
          )}
        </div>

        <div className="space-y-2">
          <label
            htmlFor="contribution-amount"
            className="block text-sm font-medium"
          >
            Amount (rands)
          </label>

          <input
            id="contribution-amount"
            type="text"
            inputMode="decimal"
            autoComplete="off"
            placeholder="500.00"
            {...register('amount')}
            aria-invalid={Boolean(errors.amount)}
            aria-describedby={
              errors.amount
                ? 'contribution-amount-error'
                : undefined
            }
            disabled={isSubmitting}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground"
          />

          {errors.amount && (
            <p
              id="contribution-amount-error"
              className="text-sm text-destructive"
              role="alert"
            >
              {errors.amount.message}
            </p>
          )}
        </div>

        {submissionError && (
          <p
            role="alert"
            className="rounded-lg border border-destructive p-3 text-sm text-destructive"
          >
            {submissionError}
          </p>
        )}

        {confirmation && (
          <p
            role="status"
            className="rounded-lg border border-border p-3 text-sm"
          >
            {confirmation}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-lime-400 px-4 py-3 font-semibold text-lime-950 transition hover:bg-lime-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isSubmitting
            ? 'Recording contribution…'
            : 'Record contribution'}
        </button>
      </form>
    </section>
  )
}