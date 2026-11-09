import {
  act,
  render,
  screen,
  waitFor,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import {
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query'

import {
  recordContributionAction,
  type RecordContributionState,
} from '@/actions/contributions'
import { RecordContributionForm } from './RecordContributionForm'

vi.mock('@/actions/contributions', () => ({
  recordContributionAction: vi.fn(),
}))

const action = vi.mocked(recordContributionAction)

const stokvelId =
  '123e4567-e89b-42d3-a456-426614174001'

const cycleId =
  '123e4567-e89b-42d3-a456-426614174002'

const memberId =
  '123e4567-e89b-42d3-a456-426614174003'

const anotherMemberId =
  '123e4567-e89b-42d3-a456-426614174004'

const initialKey =
  '123e4567-e89b-42d3-a456-426614174005'

type SubmittedContribution = {
  amount: string
  userId: string
  idempotencyKey: string
}

function submittedData(
  formData: FormData,
): SubmittedContribution {
  return {
    amount: String(formData.get('amount')),
    userId: String(formData.get('userId')),
    idempotencyKey: String(
      formData.get('idempotencyKey'),
    ),
  }
}

function response(
  status: RecordContributionState['status'],
  key: string,
  options: Partial<RecordContributionState> = {},
): RecordContributionState {
  return {
    status,
    attemptKey: key,
    nextKey: null,
    message: null,
    reference: null,
    fieldErrors: {},
    ...options,
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void

  const promise = new Promise<T>(complete => {
    resolve = complete
  })

  return { promise, resolve }
}

function renderForm(
  role: 'Member' | 'Treasurer' = 'Member',
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })

  const result = render(
    <QueryClientProvider client={queryClient}>
      <RecordContributionForm
        stokvelId={stokvelId}
        cycleId={cycleId}
        cycleNumber={1}
        currentUserId={memberId}
        currentUserName="Sipho Mokoena"
        memberRole={role}
        members={[
          {
            id: memberId,
            fullName: 'Sipho Mokoena',
          },
          {
            id: anotherMemberId,
            fullName: 'Thandi Dlamini',
          },
        ]}
        initialKey={initialKey}
      />
    </QueryClientProvider>,
  )

  return {
    ...result,
    queryClient,
  }
}

beforeEach(() => {
  action.mockReset()
})

describe('RecordContributionForm', () => {
  it('shows an amount error under the correct field and preserves the entry', async () => {
    const user = userEvent.setup()

    action.mockImplementation(async (_previous, formData) => {
      const submitted = submittedData(formData)

      return response(
        'error',
        submitted.idempotencyKey,
        {
          message:
            'Please correct the highlighted information.',
          fieldErrors: {
            amount: [
              'Enter an amount greater than zero.',
            ],
          },
        },
      )
    })

    renderForm()

    const amount = screen.getByRole('textbox', {
      name: 'Amount (rands)',
    })

    await user.type(amount, '0')
    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByText(
        'Enter an amount greater than zero.',
      ),
    ).toBeInTheDocument()

    expect(amount).toHaveValue('0')
    expect(amount).toHaveAttribute(
      'aria-invalid',
      'true',
    )

    expect(amount).toHaveAttribute(
      'aria-describedby',
      'contribution-amount-error',
    )
  })

  it('maps the API userId error to the visible Member field', async () => {
    const user = userEvent.setup()

    action.mockImplementation(async (_previous, formData) => {
      const submitted = submittedData(formData)

      return response(
        'error',
        submitted.idempotencyKey,
        {
          message:
            'Please correct the highlighted information.',
          fieldErrors: {
            userId: [
              'Select a valid stokvel member.',
            ],
          },
        },
      )
    })

    renderForm('Treasurer')

    await user.selectOptions(
      screen.getByRole('combobox', {
        name: 'Member',
      }),
      anotherMemberId,
    )

    await user.type(
      screen.getByRole('textbox', {
        name: 'Amount (rands)',
      }),
      '150,50',
    )

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByText(
        'Select a valid stokvel member.',
      ),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('combobox', {
        name: 'Member',
      }),
    ).toHaveValue(anotherMemberId)

    expect(
      screen.getByRole('textbox', {
        name: 'Amount (rands)',
      }),
    ).toHaveValue('150,50')

    expect(
      screen.getByRole('combobox', {
        name: 'Member',
      }),
    ).toHaveAttribute(
      'aria-describedby',
      'contribution-user-error',
    )
  })

  it('shows a conflict and its reference without clearing the amount', async () => {
    const user = userEvent.setup()

    action.mockImplementation(async (_previous, formData) => {
      const submitted = submittedData(formData)

      return response(
        'error',
        submitted.idempotencyKey,
        {
          message:
            'This contribution conflicts with an existing record.',
          reference: 'CORR-409-TEST',
        },
      )
    })

    renderForm()

    await user.type(
      screen.getByRole('textbox', {
        name: 'Amount (rands)',
      }),
      '150.50',
    )

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent(
      'This contribution conflicts',
    )

    expect(
      screen.getByText('Reference: CORR-409-TEST'),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('textbox', {
        name: 'Amount (rands)',
      }),
    ).toHaveValue('150.50')
  })

  it('sends only one request while a contribution is pending', async () => {
    const user = userEvent.setup()
    const pendingResult = deferred<RecordContributionState>()

    
let submittedKey = ''

    action.mockImplementation((_previous, formData) => {
    submittedKey = String(
        formData.get('idempotencyKey'),
    )

    return pendingResult.promise
    })


    renderForm()

    await user.type(
      screen.getByRole('textbox', {
        name: 'Amount (rands)',
      }),
      '150.50',
    )

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByRole('button', {
        name: 'Recording contribution...',
      }),
    ).toBeDisabled()

    expect(action).toHaveBeenCalledTimes(1)

    await user.click(
      screen.getByRole('button', {
        name: 'Recording contribution...',
      }),
    )

    expect(action).toHaveBeenCalledTimes(1)

    await act(async () => {
      pendingResult.resolve(
        response('success', submittedKey, {
          nextKey: crypto.randomUUID(),
          message: 'R 150,50 recorded successfully.',
        }),
      )

      await pendingResult.promise
    })

    expect(
      await screen.findByRole('status'),
    ).toHaveTextContent('recorded successfully')
  })

  it('reuses the key after an uncertain result and changes it after success', async () => {
    const user = userEvent.setup()
    const submitted: SubmittedContribution[] = []

    let attempt = 0

    action.mockImplementation(async (_previous, formData) => {
      const data = submittedData(formData)
      submitted.push(data)
      attempt += 1

      if (attempt === 1) {
        return response(
          'uncertain',
          data.idempotencyKey,
          {
            message:
              'The connection was interrupted. Retry with the same details.',
          },
        )
      }

      return response(
        'success',
        data.idempotencyKey,
        {
          nextKey:
            '123e4567-e89b-42d3-a456-426614174099',
          message: 'Contribution recorded successfully.',
        },
      )
    })

    renderForm()

    const amount = screen.getByRole('textbox', {
      name: 'Amount (rands)',
    })

    await user.type(amount, '150.50')

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByText(
        'The connection was interrupted. Retry with the same details.',
      ),
    ).toBeInTheDocument()

    expect(amount).toHaveValue('150.50')

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    expect(
      await screen.findByText(
        'Contribution recorded successfully.',
      ),
    ).toBeInTheDocument()

    expect(submitted).toHaveLength(2)

    expect(submitted[0].idempotencyKey).toBe(
      submitted[1].idempotencyKey,
    )

    expect(submitted[0].amount).toBe('150.50')
    expect(submitted[1].amount).toBe('150.50')

    await user.type(amount, '200')

    await user.click(
      screen.getByRole('button', {
        name: 'Record contribution',
      }),
    )

    await waitFor(() => {
      expect(submitted).toHaveLength(3)
    })

    expect(submitted[2].idempotencyKey).not.toBe(
      submitted[1].idempotencyKey,
    )

    expect(submitted[2].amount).toBe('200')
  })
})
