import {
  render,
  screen,
} from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { JoinHeading } from './JoinHeading'
import { JoinProgress } from './JoinProgress'

describe('joining wizard accessibility', () => {
  it('focuses the current step heading', () => {
    render(
      <>
        <JoinHeading step="rules" />
        <JoinProgress step="rules" />
      </>,
    )

    const heading = screen.getByRole('heading', {
      level: 1,
      name: 'Step 2 of 3: Review the stokvel rules',
    })

    expect(heading).toHaveFocus()
    expect(
      screen.getByText('Rules and amount').closest('li'),
    ).toHaveAttribute('aria-current', 'step')
  })

  it('updates focus after moving to confirmation', () => {
    const { rerender } = render(
      <>
        <JoinHeading step="rules" />
        <JoinProgress step="rules" />
      </>,
    )

    rerender(
      <>
        <JoinHeading step="confirm" />
        <JoinProgress step="confirm" />
      </>,
    )

    expect(
      screen.getByRole('heading', {
        name: 'Step 3 of 3: Confirm your membership',
      }),
    ).toHaveFocus()

    expect(
      screen.getByText('Confirm join').closest('li'),
    ).toHaveAttribute('aria-current', 'step')
  })

  it('keeps only one progress step current', () => {
    render(<JoinProgress step="code" />)

    expect(
      screen.getAllByText(/Step [123]/),
    ).toHaveLength(3)

    expect(
      document.querySelectorAll(
        '[aria-current="step"]',
      ),
    ).toHaveLength(1)
  })
})
