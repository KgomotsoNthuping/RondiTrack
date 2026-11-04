'use client'

import { useEffect, useRef } from 'react'
import type { JoinStep } from '@/lib/joinFlow'

const headings = {
  code: 'Check your invitation code',
  rules: 'Review the stokvel rules',
  confirm: 'Confirm your membership',
}

const stepNumbers = {
  code: 1,
  rules: 2,
  confirm: 3,
}

export function JoinHeading({
  step,
}: {
  step: JoinStep
}) {
  const headingRef = useRef<HTMLHeadingElement>(null)

  useEffect(() => {
    headingRef.current?.focus()
  }, [step])

  return (
    <h1
      ref={headingRef}
      tabIndex={-1}
      className="text-2xl font-extrabold outline-none sm:text-3xl"
    >
      Step {stepNumbers[step]} of 3: {headings[step]}
    </h1>
  )
}
