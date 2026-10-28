export function extractRefreshToken(
  setCookieHeaders: readonly string[],
): string | null {
  for (const header of setCookieHeaders) {
    const match =
      /^refresh_token=([^;\s]+)(?:;|$)/.exec(
        header.trim(),
      )

    if (match) {
      return match[1]
    }
  }

  return null
}