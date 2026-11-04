
export const MAX_PROOF_BYTES = 5 * 1024 * 1024

export const PROOF_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
] as const

export type ProofFileInfo = {
  name: string
  size: number
  type: string
}

export function validateProofFile(
  file: ProofFileInfo | null,
): string | null {
  if (!file || file.size === 0) {
    return 'Choose a PDF, JPEG or PNG file.'
  }

  if (file.size > MAX_PROOF_BYTES) {
    return 'The file must not exceed 5 MiB.'
  }

  if (!PROOF_TYPES.some(type => type === file.type)) {
    return 'Only PDF, JPEG and PNG files are accepted.'
  }

  return null
}

export function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
    value,
  )
}

export function proofPath(
  stokvelId: string,
  contributionId: string,
): string {
  return `/api/v1/stokvels/${stokvelId}/contributions/${contributionId}/proof`
}

export function proofDownloadPath(
  stokvelId: string,
  contributionId: string,
): string {
  return `/bff/stokvels/${stokvelId}/contributions/${contributionId}/proof`
}

export function proofDownloadHeaders(
  contentType: string,
  contributionId: string,
): Headers | null {
  const extensions: Record<string, string> = {
    'application/pdf': 'pdf',
    'image/jpeg': 'jpg',
    'image/png': 'png',
  }

  const extension = extensions[contentType]

  if (!extension || !isUuid(contributionId)) {
    return null
  }

  return new Headers({
    'Content-Type': contentType,
    'Content-Disposition':
      `attachment; filename="proof-${contributionId}.${extension}"`,
    'X-Content-Type-Options': 'nosniff',
    'Cache-Control': 'private, no-store',
  })
}
