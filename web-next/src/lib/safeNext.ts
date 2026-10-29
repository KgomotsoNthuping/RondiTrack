export function safeNext(value: unknown): string | null {
  if (typeof value !== 'string' || !value) {
    return null
  }

  // Require exactly one leading slash.
  if (!value.startsWith('/') || /^\/[\\/]/.test(value)) {
    return null
  }

  // Prevent whitespace/control characters and backslashes
  // from changing how browsers interpret the destination.
  if (/[\u0000-\u0020\u007f\\]/.test(value)) {
    return null
  }

  const pathname = value.split(/[?#]/, 1)[0]

  // Reject encoded separators in the path.
  if (/%(?:2f|5c)/i.test(pathname)) {
    return null
  }

  // Avoid redirecting a successful login back to an auth form.
  if (
    pathname === '/login' ||
    pathname === '/register'
  ) {
    return null
  }

  try {
    const base = 'https://ronditrack.invalid'
    const destination = new URL(value, base)

    if (destination.origin !== base) {
      return null
    }

    return value
  } catch {
    return null
  }
}
