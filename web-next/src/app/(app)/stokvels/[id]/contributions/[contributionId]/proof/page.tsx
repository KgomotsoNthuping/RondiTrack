import { Suspense } from 'react'
import Link from 'next/link'
import { notFound, redirect } from 'next/navigation'
import { ArrowLeft, FileText } from 'lucide-react'

import Loading from '@/app/(app)/loading'
import { ApiError } from '@/api/ApiError'
import { ApiErrorPanel } from '@/components/ApiErrorPanel'
import { ProofUploadForm } from '@/components/ProofUploadForm'
import { ProofDeleteForm } from '@/components/ProofDeleteForm'
import {
  getProofContribution,
  getProofMetadata,
} from '@/lib/proof-data'
import { getStokvelDetail } from '@/lib/stokvel-data'
import { getSession } from '@/lib/session'
import {
  isUuid,
  proofDownloadPath,
} from '@/lib/proofFiles'
import { formatCents } from '@/lib/money'
import { formatDateTime } from '@/lib/dates'

type Props = {
  params: Promise<{
    id: string
    contributionId: string
  }>
}

async function ProofContent({
  params,
}: Props) {
  const { id, contributionId } = await params

  if (
    !isUuid(id) ||
    !isUuid(contributionId)
  ) {
    notFound()
  }

  const session = await getSession()

  if (!session) {
    redirect('/login')
  }

  const [stokvel, contribution] =
    await Promise.all([
      getStokvelDetail(id),
      getProofContribution(id, contributionId),
    ])

  if (contribution instanceof ApiError) {
    return <ApiErrorPanel error={contribution} />
  }

  const owner =
    contribution.userId === session.userId

  const treasurer =
    stokvel.role === 'Treasurer'

  if (!owner && !treasurer) {
    notFound()
  }

  const proof = await getProofMetadata(
    id,
    contributionId,
  )

  if (proof instanceof ApiError) {
    return <ApiErrorPanel error={proof} />
  }

  const openCycle =
    stokvel.currentCycle?.id ===
      contribution.contributionCycleId &&
    stokvel.currentCycle.status === 'Open'

  const canEdit = owner && openCycle

  return (
    <div className="mx-auto max-w-3xl space-y-7">
      <Link
        href={`/stokvels/${id}/contributions`}
        className="inline-flex min-h-11 items-center gap-2 font-semibold text-lime-800 hover:underline"
      >
        <ArrowLeft
          aria-hidden="true"
          className="size-4"
        />
        Back to contribution history
      </Link>

      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-lime-700">
          Proof of payment
        </p>

        <h1 className="mt-2 text-3xl font-extrabold">
          Contribution document
        </h1>

        <p className="mt-2 text-slate-600">
          {stokvel.name}
        </p>
      </header>

      <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="text-lg font-bold">
          Contribution details
        </h2>

        <p className="mt-3 text-slate-700">
          Amount:{' '}
          <strong>
            {formatCents(
              Math.round(contribution.amount * 100),
            )}
          </strong>
        </p>

        <p className="mt-2 text-sm text-slate-600">
          {openCycle
            ? 'Current cycle: Open'
            : 'Cycle: Closed or no longer current'}
        </p>
      </section>

      <section
        aria-labelledby="existing-proof-title"
        className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
      >
        <div className="flex items-center gap-3">
          <FileText
            aria-hidden="true"
            className="size-6 text-lime-700"
          />

          <h2
            id="existing-proof-title"
            className="text-xl font-bold"
          >
            Stored proof
          </h2>
        </div>

        {proof ? (
          <div className="mt-5 space-y-4">
            <p className="text-slate-700">
              A proof-of-payment document is available.
            </p>

            <dl className="space-y-2 text-sm">
              <div>
                <dt className="font-semibold">
                  File type
                </dt>
                <dd>{proof.contentType}</dd>
              </div>

              <div>
                <dt className="font-semibold">
                  File size
                </dt>
                <dd>
                  {(proof.sizeBytes / 1024).toFixed(1)} KiB
                </dd>
              </div>

              <div>
                <dt className="font-semibold">
                  Uploaded
                </dt>
                <dd>
                  {formatDateTime(
                    proof.uploadedAtUtc,
                  )}
                </dd>
              </div>
            </dl>

            <a
              href={proofDownloadPath(
                id,
                contributionId,
              )}
              className="inline-flex min-h-11 items-center rounded-xl bg-lime-400 px-5 py-3 font-bold text-lime-950 hover:bg-lime-300"
            >
              Download proof
            </a>

            {canEdit && (
              <ProofDeleteForm
                stokvelId={id}
                contributionId={contributionId}
              />
            )}
          </div>
        ) : (
          <p className="mt-4 text-slate-600">
            No proof-of-payment document has been
            uploaded for this contribution.
          </p>
        )}
      </section>

      {canEdit ? (
        <ProofUploadForm
          stokvelId={id}
          contributionId={contributionId}
          replacing={proof !== null}
        />
      ) : (
        <section
          role="status"
          className="rounded-xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-700"
        >
          {owner
            ? 'This contribution cycle is closed. Existing proof may be downloaded, but it cannot be replaced or removed.'
            : 'As Treasurer, you may review this proof. Only its contribution owner can upload, replace or remove it.'}
        </section>
      )}
    </div>
  )
}

export default function ContributionProofPage({
  params,
}: Props) {
  return (
    <Suspense fallback={<Loading />}>
      <ProofContent params={params} />
    </Suspense>
  )
}
