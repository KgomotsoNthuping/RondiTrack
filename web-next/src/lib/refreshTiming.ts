export const DEFAULT_REFRESH_MARGIN_MS = 60_000

export function needsRefresh(
  expiresAtUtc: string,
  now: number = Date.now(),
  marginMs: number = DEFAULT_REFRESH_MARGIN_MS,
): boolean {
  const expiresAt = Date.parse(expiresAtUtc)

  if (!Number.isFinite(expiresAt)) {
    return true
  }

  return expiresAt - now <= marginMs
}

export function isAccessTokenExpired(
  expiresAtUtc: string,
  now: number = Date.now(),
): boolean {
  const expiresAt = Date.parse(expiresAtUtc)

  if (!Number.isFinite(expiresAt)) {
    return true
  }

  return expiresAt <= now
}
