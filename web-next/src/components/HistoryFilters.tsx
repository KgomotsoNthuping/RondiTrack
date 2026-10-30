'use client'

import { useTransition } from 'react'
import { useQueryStates } from 'nuqs'

import { historyFilters } from '../lib/historyFilters'
import type { RecordableMember } from '../lib/contributionMembers'

type HistoryFiltersProps = {
  members: RecordableMember[]
}

export function HistoryFilters({
  members,
}: HistoryFiltersProps) {
  const [isUpdating, startTransition] =
    useTransition()

  const [{ member, from, to }, setFilters] =
    useQueryStates(historyFilters, {
      shallow: false,
      history: 'push',
      clearOnDefault: true,
      startTransition,
    })

  const inputClass =
    'min-h-11 w-full rounded-lg border border-slate-300 ' +
    'bg-white px-3 py-2 text-sm text-slate-950 ' +
    'outline-none focus-visible:ring-2 focus-visible:ring-lime-500'

  return (
    <section
      aria-labelledby="history-filters-title"
      className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
    >
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h2
          id="history-filters-title"
          className="text-lg font-bold"
        >
          Filter contributions
        </h2>

        <button
          type="button"
          onClick={() =>
            void setFilters({
              member: null,
              from: null,
              to: null,
            })
          }
          disabled={isUpdating}
          className="min-h-11 rounded-lg px-4 text-sm font-semibold text-lime-800 hover:bg-lime-50 disabled:opacity-60"
        >
          Clear filters
        </button>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="space-y-2">
          <label
            htmlFor="history-member"
            className="block text-sm font-semibold"
          >
            Member
          </label>

          <select
            id="history-member"
            value={member ?? ''}
            disabled={isUpdating}
            onChange={event =>
              void setFilters({
                member: event.target.value || null,
              })
            }
            className={inputClass}
          >
            <option value="">
              Everyone
            </option>

            {members.map(person => (
              <option
                key={person.id}
                value={person.id}
              >
                {person.fullName}
              </option>
            ))}
          </select>
        </div>

        <div className="space-y-2">
          <label
            htmlFor="history-from"
            className="block text-sm font-semibold"
          >
            From date
          </label>

          <input
            id="history-from"
            type="date"
            value={from ?? ''}
            disabled={isUpdating}
            onChange={event =>
              void setFilters({
                from: event.target.value || null,
              })
            }
            className={inputClass}
          />
        </div>

        <div className="space-y-2">
          <label
            htmlFor="history-to"
            className="block text-sm font-semibold"
          >
            To date
          </label>

          <input
            id="history-to"
            type="date"
            value={to ?? ''}
            disabled={isUpdating}
            onChange={event =>
              void setFilters({
                to: event.target.value || null,
              })
            }
            className={inputClass}
          />
        </div>
      </div>

      <p className="mt-4 text-sm text-slate-600">
        Dates use South African time.
        Both selected calendar days are included.
      </p>

      <p
        role="status"
        aria-live="polite"
        className="mt-2 min-h-5 text-sm font-medium text-lime-800"
      >
        {isUpdating
          ? 'Updating contribution history...'
          : ''}
      </p>
    </section>
  )
}
