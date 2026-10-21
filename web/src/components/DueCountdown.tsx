import { useEffect, useState } from 'react'
import { Clock3 } from 'lucide-react'
import { dueCountdownLabel, millisecondsUntilNextSaMidnight } from '@/lib/countdown'

type Props = { dueDate: string | null | undefined; status: string | null | undefined }

export function DueCountdown({ dueDate, status }: Props) {
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    // Re-arm after each tick: once per Johannesburg midnight, not each second.
    const timeout = window.setTimeout(() => setNow(new Date()), millisecondsUntilNextSaMidnight(now) + 50)
    return () => window.clearTimeout(timeout)
  }, [now])

  // Server-provided cycle status wins over the device clock.
  if (status !== 'Open' || !dueDate) return null
  return <span className="inline-flex flex-wrap items-center gap-1.5 text-sm font-semibold text-foreground">
    <Clock3 aria-hidden className="size-4 shrink-0" />{dueCountdownLabel(dueDate, now)}
  </span>
}
