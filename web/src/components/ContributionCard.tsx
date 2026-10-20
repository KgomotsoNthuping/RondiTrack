
import type { ContributionItem } from '../api/types'
import { formatCents } from '../lib/money'
import { formatDateTime } from '../lib/dates'
import { contributionStatusLabel } from '../lib/contributionStatus'

type ContributionCardProps = {
  contribution: ContributionItem
}

export function ContributionCard({
  contribution,
}: ContributionCardProps) {
  return (
    <article className="contribution-card">
      <div className="contribution-card__header">
        <h3>
          {formatCents(contribution.amountInCents)}
        </h3>

        <span className="contribution-card__status">
          {contributionStatusLabel(
            contribution.status,
          )}
        </span>
      </div>

      <p>
        Cycle {contribution.cycleNumber}
      </p>

      <p>
        Paid at:{' '}
        {contribution.paidAtUtc != null
          ? formatDateTime(
              contribution.paidAtUtc,
            )
          : 'Not paid yet'}
      </p>
    </article>
  )
}
