import { describe, expect, it } from 'vitest'
import {
  MAX_PROOF_BYTES,
  proofDownloadHeaders,
  proofDownloadPath,
  validateProofFile,
} from './proofFiles'

describe('proof of payment validation', () => {
  it('accepts a PDF just below the limit', () => {
    expect(
      validateProofFile({
        name: 'payment.pdf',
        type: 'application/pdf',
        size: MAX_PROOF_BYTES - 1,
      }),
    ).toBeNull()
  })

  it('accepts JPEG and PNG files', () => {
    for (const type of ['image/jpeg', 'image/png']) {
      expect(
        validateProofFile({
          name: 'payment',
          type,
          size: 1024,
        }),
      ).toBeNull()
    }
  })

  it('rejects a file over the limit', () => {
    expect(
      validateProofFile({
        name: 'large.pdf',
        type: 'application/pdf',
        size: MAX_PROOF_BYTES + 1,
      }),
    ).toMatch(/5 MiB/)
  })

  it('rejects an empty file', () => {
    expect(validateProofFile(null)).not.toBeNull()
  })

  it('rejects an unsupported MIME type', () => {
    expect(
      validateProofFile({
        name: 'payment.txt',
        type: 'text/plain',
        size: 100,
      }),
    ).not.toBeNull()
  })

  it('sets private download headers', () => {
    const headers = proofDownloadHeaders(
      'application/pdf',
      '123e4567-e89b-42d3-a456-426614174001',
    )

    expect(headers?.get('Cache-Control')).toBe(
      'private, no-store',
    )

    expect(
      headers?.get('X-Content-Type-Options'),
    ).toBe('nosniff')

    expect(
      headers?.get('Content-Disposition'),
    ).toContain('attachment;')
  })

  it('rejects unrecognised download formats', () => {
    expect(
      proofDownloadHeaders(
        'text/html',
        '123e4567-e89b-42d3-a456-426614174001',
      ),
    ).toBeNull()
  })

  it('uses the local BFF download path', () => {
    expect(
      proofDownloadPath(
        '123e4567-e89b-42d3-a456-426614174001',
        '123e4567-e89b-42d3-a456-426614174002',
      ),
    ).toContain('/bff/stokvels/')
  })
})
