type PagedResponse = {
  items: unknown[]
  nextPageToken: string
}

export async function fetchExtraPage<T extends PagedResponse>(
  url: string,
): Promise<T> {
  const response = await fetch(
  new URL(url, window.location.origin),
  {
    credentials: 'same-origin',
    cache: 'no-store',
  })

  if (response.status === 401) {
    const next =
      window.location.pathname +
      window.location.search

    const query = new URLSearchParams({
      expired: '1',
      next,
    })

    window.location.assign(
      `/login?${query.toString()}`,
    )

    throw new Error('Your session has ended.')
  }

  const contentType =
    response.headers.get('Content-Type') ?? ''

  let payload: unknown = null

  if (contentType.includes('json')) {
    try {
      payload = await response.json()
    } catch {
      throw new Error(
        'The server returned invalid JSON.',
      )
    }
  }

  if (!response.ok) {
    const problem =
      payload !== null &&
      typeof payload === 'object' &&
      'title' in payload &&
      typeof payload.title === 'string'
        ? payload.title
        : 'The request failed.'

    throw new Error(
      `${problem} (${response.status})`,
    )
  }

  if (
    payload === null ||
    typeof payload !== 'object' ||
    !('items' in payload) ||
    !Array.isArray(payload.items) ||
    !('nextPageToken' in payload) ||
    typeof payload.nextPageToken !== 'string'
  ) {
    throw new Error(
      'The server returned an invalid page.',
    )
  }

  return payload as T
}
