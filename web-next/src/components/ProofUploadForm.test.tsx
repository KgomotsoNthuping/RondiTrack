import {
  render,
  screen,
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
  uploadProofAction,
  initialProofState,
} from '@/actions/proofs'
import { MAX_PROOF_BYTES } from '@/lib/proofFiles'
import { ProofUploadForm } from './ProofUploadForm'

vi.mock('@/actions/proofs', () => ({
  uploadProofAction: vi.fn(),
  initialProofState: {
    status: 'idle',
    resultId: '',
    message: null,
    fileError: null,
    reference: null,
  },
}))

const action = vi.mocked(uploadProofAction)

const stokvelId =
  '123e4567-e89b-42d3-a456-426614174001'

const contributionId =
  '123e4567-e89b-42d3-a456-426614174002'

function renderForm() {
  return render(
    <ProofUploadForm
      stokvelId={stokvelId}
      contributionId={contributionId}
      replacing={false}
    />,
  )
}

beforeEach(() => {
  action.mockReset()
})

describe('proof-of-payment uploads', () => {
  it('rejects a file exceeding the size limit', async () => {
    const user = userEvent.setup({
      applyAccept: false,
    })

    renderForm()

    const oversized = new File(
      [new Uint8Array(MAX_PROOF_BYTES + 1)],
      'receipt.pdf',
      { type: 'application/pdf' },
    )

    await user.upload(
      screen.getByLabelText('Proof document'),
      oversized,
    )

    expect(
      screen.getByRole('alert'),
    ).toHaveTextContent('5 MiB')

    expect(
      screen.getByLabelText('Proof document'),
    ).toHaveAttribute('aria-invalid', 'true')

    await user.click(
      screen.getByRole('button', {
        name: 'Upload proof',
      }),
    )

    expect(action).not.toHaveBeenCalled()
  })

  it('rejects an unsupported file type', async () => {
    const user = userEvent.setup({
      applyAccept: false,
    })

    renderForm()

    await user.upload(
      screen.getByLabelText('Proof document'),
      new File(
        ['not a proof'],
        'notes.txt',
        { type: 'text/plain' },
      ),
    )

    expect(
      screen.getByRole('alert'),
    ).toHaveTextContent(
      'Only PDF, JPEG and PNG files are accepted.',
    )

    expect(action).not.toHaveBeenCalled()
  })

  it('shows the API file error under the file input', async () => {
    const user = userEvent.setup()

    action.mockImplementation(async () => ({
      ...initialProofState,
      status: 'invalid',
      resultId: 'invalid-1',
      message: 'Please check the selected file.',
      fileError:
        'The uploaded file is not a genuine PDF.',
    }))

    renderForm()

    await user.upload(
      screen.getByLabelText('Proof document'),
      new File(
        ['%PDF-1.7'],
        'receipt.pdf',
        { type: 'application/pdf' },
      ),
    )

    await user.click(
      screen.getByRole('button', {
        name: 'Upload proof',
      }),
    )

    expect(
      await screen.findByText(
        'The uploaded file is not a genuine PDF.',
      ),
    ).toBeInTheDocument()
  })

  it('announces an unexpected API error with its reference', async () => {
    const user = userEvent.setup()

    action.mockImplementation(async () => ({
      ...initialProofState,
      status: 'unexpected',
      resultId: 'failed-1',
      message: 'The proof service is unavailable.',
      reference: 'CORR-500-TEST',
    }))

    renderForm()

    await user.upload(
      screen.getByLabelText('Proof document'),
      new File(
        ['%PDF-1.7'],
        'receipt.pdf',
        { type: 'application/pdf' },
      ),
    )

    await user.click(
      screen.getByRole('button', {
        name: 'Upload proof',
      }),
    )

    const alert = await screen.findByRole('alert')

    expect(alert).toHaveTextContent(
      'The proof service is unavailable.',
    )

    expect(alert).toHaveTextContent(
      'CORR-500-TEST',
    )

    expect(
      screen.getByRole('button', {
        name: 'Dismiss notification',
      }),
    ).toBeInTheDocument()
  })
})
