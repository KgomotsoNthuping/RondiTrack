import type { JoinStep } from '@/lib/joinFlow'

const steps = [
  { id: 'code', label: 'Invitation code' },
  { id: 'rules', label: 'Rules and amount' },
  { id: 'confirm', label: 'Confirm join' },
] as const

export function JoinProgress({
  step,
}: {
  step: JoinStep
}) {
  return (
    <nav aria-label="Joining progress">
      <ol className="grid grid-cols-3 gap-2">
        {steps.map((item, index) => {
          const active = item.id === step

          return (
            <li
              key={item.id}
              aria-current={active ? 'step' : undefined}
              className={
                active
                  ? 'rounded-xl border-2 border-lime-600 bg-lime-50 p-3 text-center text-lime-950'
                  : 'rounded-xl border border-slate-200 bg-slate-50 p-3 text-center text-slate-600'
              }
            >
              <span className="block text-xs font-semibold">
                Step {index + 1}
              </span>
              <span className="mt-1 block text-xs sm:text-sm">
                {item.label}
              </span>
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
