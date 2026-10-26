import { useRef, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { ApiError } from '../api/ApiError'
import { PaymentIntentTracker } from '../contributions/paymentIntent'
import { parseRetryAfter } from '../lib/errorMessages'
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
  cycleId: string
  cycleNumber: number
  cycleStatus: string
  amountOwedInCents: number
  onRecord: (
    values: ContributionRequestValues & {
      idempotencyKey: string
    },
  ) => Promise<void>
}

type ProblemResponse = {
  title?: string
  detail?: string
  correlationId?: string
  errors?: Record<string, string[]>
}

function readProblem(error: ApiError): ProblemResponse {
  const value: unknown = error.problem

  if (!value || typeof value !== 'object') {
    return {}
  }

  return value as ProblemResponse
}

function messageForError(
  error: unknown,
  problem: ProblemResponse,
): string {
  if (!(error instanceof ApiError) || error.status === 0) {
    return 'The connection was interrupted. Your contribution may have been recorded. Retry with the same details to check safely.'
  }

  if (error.status === 429) {
    const seconds = parseRetryAfter(error.retryAfter)

    return seconds === null
      ? 'Too many requests. Wait before trying again.'
      : `Too many requests. Try again in ${seconds} seconds.`
  }

  if (error.status >= 500) {
    return 'The server could not confirm the result. Your contribution may have been recorded. Retry with the same details to check safely.'
  }

  if (error.status === 409 || error.status === 422) {
    return (
      problem.detail ??
      'The contribution could not be recorded.'
    )
  }

  if (error.status === 403) {
    return 'You do not have permission to record this contribution.'
  }

  if (error.status === 404) {
    return 'This stokvel or contribution cycle was not found.'
  }

  return (
    problem.detail ??
    problem.title ??
    'The contribution could not be recorded.'
  )
}

export function RecordContributionForm({
  currentUserId,
  currentUserName,
  memberRole,
  members,
  cycleId,
  cycleNumber,
  cycleStatus,
  amountOwedInCents,
  onRecord,
}: RecordContributionFormProps) {
  const intentRef = useRef(new PaymentIntentTracker())
  const submittingRef = useRef(false)

  const [confirmation, setConfirmation] = useState<
    string | null
  >(null)

  const [submissionError, setSubmissionError] = useState<
    string | null
  >(null)

  const isTreasurer = memberRole === 'Treasurer'
  const isOpen = cycleStatus === 'Open'

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    clearErrors,
    setFocus,
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

  function changeIntent() {
    intentRef.current.clear()
    setConfirmation(null)
    setSubmissionError(null)
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

      const idempotencyKey = intentRef.current.keyFor({
        cycleId,
        userId: request.userId,
        amountInCents: request.amountInCents,
      })

      await onRecord({
        ...request,
        idempotencyKey,
      })

      const memberName =
        members.find(member => member.id === request.userId)
          ?.fullName ?? currentUserName

      intentRef.current.clear()

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
    } catch (error) {
      if (error instanceof ApiError) {
        const problem = readProblem(error)
        const unknownFields: string[] = []
        let firstInvalidField: 'userId' | 'amount' | null = null

        for (const [key, messages] of Object.entries(
          problem.errors ?? {},
        )) {
          const field = key.toLowerCase().replaceAll(
            /[^a-z]/g,
            '',
          )
          const message = messages.join(' ')

          if (field.endsWith('userid')) {
            setError('userId', {
              type: 'server',
              message,
            })

            firstInvalidField ??= 'userId'
          } else if (
            field.endsWith('amount') ||
            field.endsWith('amountincents')
          ) {
            setError('amount', {
              type: 'server',
              message,
            })

            firstInvalidField ??= 'amount'
          } else {
            unknownFields.push(`${key}: ${message}`)
          }
        }

        const reference = problem.correlationId
          ? ` Reference: ${problem.correlationId}.`
          : ''

        setSubmissionError(
          [
            messageForError(error, problem),
            ...unknownFields,
          ].join(' ') + reference,
        )

        if (firstInvalidField) {
          setFocus(firstInvalidField)
        }
      } else {
        setSubmissionError(messageForError(error, {}))
      }
    } finally {
      submittingRef.current = false
    }
  }

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

  return (
    <section className="rounded-xl border border-border bg-card p-5 text-card-foreground shadow-sm">
      <div className="mb-5 space-y-2">
        <h2 className="text-lg font-semibold">
          Record contribution
        </h2>
        <p className="text-sm text-muted-foreground">
          Cycle {cycleNumber}
        </p>
        <p className="text-sm">
          Amount outstanding:{' '}
          <strong>
            {formatCents(amountOwedInCents)}
          </strong>
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
              {...register('userId', {
                onChange: changeIntent,
              })}
              disabled={isSubmitting}
              aria-invalid={Boolean(errors.userId)}
              aria-describedby={
                errors.userId
                  ? 'contribution-user-error'
                  : undefined
              }
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground"
            >
              <option value="">Select member</option>
              {members.map(member => (
                <option
                  key={member.id}
                  value={member.id}
                >
                  {member.fullName}
                </option>
              ))}
            </select>
          ) : (
            <>
              <p className="rounded-lg border border-input bg-background px-3 py-2 text-sm">
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
              role="alert"
              className="text-sm text-destructive"
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
            {...register('amount', {
              onChange: changeIntent,
            })}
            disabled={isSubmitting}
            aria-invalid={Boolean(errors.amount)}
            aria-describedby={
              errors.amount
                ? 'contribution-amount-error'
                : undefined
            }
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-foreground"
          />

          {errors.amount && (
            <p
              id="contribution-amount-error"
              role="alert"
              className="text-sm text-destructive"
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
