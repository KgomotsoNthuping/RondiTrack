
import {
  Check,
  CheckCheck,
  CircleHelp,
  Clock3,
  LockKeyhole,
  Send,
  AlertTriangle,
} from "lucide-react"

import { cn } from "@/lib/utils"

type StatusBadgeProps = {
  status: string | null | undefined
  kind: "cycle" | "contribution"
  className?: string
}

type StatusStyle = {
  label: string
  icon: typeof Check
  classes: string
}

const cycleStyles: Record<string, StatusStyle> = {
  Open: {
    label: "Open",
    icon: Clock3,
    classes:
      "bg-lime-200 text-lime-950 dark:bg-lime-300 dark:text-lime-950",
  },
  Closed: {
    label: "Closed",
    icon: LockKeyhole,
    classes:
      "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-white",
  },
  PaidOut: {
    label: "Paid out",
    icon: Send,
    classes:
      "bg-blue-100 text-blue-950 dark:bg-blue-300 dark:text-blue-950",
  },
}

const contributionStyles: Record<string, StatusStyle> = {
  Pending: {
    label: "Pending",
    icon: Clock3,
    classes:
      "bg-amber-100 text-amber-950 dark:bg-amber-300 dark:text-amber-950",
  },
  Paid: {
    label: "Paid",
    icon: Check,
    classes:
      "bg-sky-100 text-sky-950 dark:bg-sky-300 dark:text-sky-950",
  },
  Confirmed: {
    label: "Confirmed",
    icon: CheckCheck,
    classes:
      "bg-lime-200 text-lime-950 dark:bg-lime-300 dark:text-lime-950",
  },
}

const fallbackStyle: StatusStyle = {
  label: "Unknown",
  icon: CircleHelp,
  classes:
    "bg-slate-100 text-slate-900 dark:bg-slate-700 dark:text-white",
}

export function StatusBadge({
  status,
  kind,
  className,
}: StatusBadgeProps) {
  const styleMap =
    kind === "cycle" ? cycleStyles : contributionStyles

  const selected = status ? styleMap[status] : undefined
  const style = selected ?? fallbackStyle
  const Icon = style.icon

  const label = selected
    ? style.label
    : status?.trim() || fallbackStyle.label

  return (
    <span
      className={cn(
        "inline-flex max-w-full items-center gap-1.5 rounded-full",
        "px-3 py-1.5 text-xs font-semibold leading-5",
        "whitespace-nowrap",
        style.classes,
        className,
      )}
    >
      <Icon aria-hidden="true" className="size-3.5 shrink-0" />
      <span>{label}</span>
    </span>
  )
}

type ArrearsBadgeProps = {
  amountInCents: number
  className?: string
}

const randFormatter = new Intl.NumberFormat("en-ZA", {
  style: "currency",
  currency: "ZAR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function ArrearsBadge({
  amountInCents,
  className,
}: ArrearsBadgeProps) {
  if (amountInCents <= 0) {
    return null
  }

  return (
    <span
      className={cn(
        "inline-flex max-w-full items-center gap-2 rounded-full",
        "bg-red-100 px-3 py-1.5 text-xs font-semibold",
        "text-red-950 dark:bg-red-300 dark:text-red-950",
        className,
      )}
    >
      <AlertTriangle
        aria-hidden="true"
        className="size-3.5 shrink-0"
      />
      <span className="min-w-0 whitespace-normal">
        In arrears:{" "}
        <span className="whitespace-nowrap">
          {randFormatter.format(amountInCents / 100)}
        </span>
      </span>
    </span>
  )
}
