
import type { MeHomeEntry } from '../api/types'
import { formatCents } from '../lib/money'
import {
  formatDate,
  formatCalendarDate,
} from '../lib/dates'
import {
  isTreasurerRole,
  roleLabel,
} from '../lib/roles'

type StokvelCardProps = {
  stokvel: MeHomeEntry
}

export function StokvelCard({
  stokvel,
}: StokvelCardProps) {
  // The user's role belongs to this specific stokvel.
  const isTreasurer = isTreasurerRole(stokvel.role)

  // Derived from props, not stored in useState.
  const hasOpenCycle =
    stokvel.cycleStatus === 'Open' &&
    stokvel.currentCycle != null

  const hasPaymentDetails =
    stokvel.paidThisCycleInCents != null &&
    stokvel.stillOwedInCents != null

  const allPaidUp =
    hasOpenCycle &&
    stokvel.stillOwedInCents === 0

  return (
    <article className="stokvel-card">
      <header className="stokvel-card__header">
        <h2>{stokvel.name}</h2>
        <p>{roleLabel(stokvel.role)}</p>
      </header>

      <div className="stokvel-card__cycle">
        {hasOpenCycle ? (
          <>
            <h3>
              Cycle {stokvel.currentCycle}
            </h3>

            <p>
              Status: {stokvel.cycleStatus}
            </p>

            <p>
              Due date:{' '}
              {stokvel.cycleDueDate != null
                ? formatCalendarDate(
                    stokvel.cycleDueDate,
                  )
                : 'No due date set'}
            </p>

            {hasPaymentDetails ? (
              <>
                <p>
                  Paid this cycle:{' '}
                  {formatCents(
                    stokvel.paidThisCycleInCents!,
                  )}
                </p>

                {allPaidUp ? (
                  <p className="stokvel-card__success">
                    All paid up for this cycle
                  </p>
                ) : (
                  <p>
                    Still owed:{' '}
                    {formatCents(
                      stokvel.stillOwedInCents!,
                    )}
                  </p>
                )}
              </>
            ) : (
              <p>
                Payment details not available
              </p>
            )}
          </>
        ) : (
          <>
            {stokvel.currentCycle != null &&
              stokvel.cycleStatus != null && (
                <p>
                  Cycle {stokvel.currentCycle}
                  {' · '}
                  {stokvel.cycleStatus}
                </p>
              )}

            <p>No open cycle</p>
          </>
        )}
      </div>

      <div className="stokvel-card__payout">
        <h3>Next payout</h3>

        <p>
          {stokvel.nextPayoutDateUtc != null
            ? formatDate(
                stokvel.nextPayoutDateUtc,
              )
            : 'No payout scheduled'}
        </p>
      </div>

      {isTreasurer && (
        <div className="stokvel-card__treasurer">
          <h3>Treasurer's next jobs</h3>

          <ul>
            <li>Close cycle (Week 8)</li>
            <li>Record payout (Week 8)</li>
          </ul>
        </div>
      )}
    </article>
  )
}
