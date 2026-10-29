import {
  type NextRequest,
  NextResponse,
} from 'next/server'

import {
  openSession,
  sealSession,
  SESSION_COOKIE,
  sessionCookieOptions,
} from '@/lib/session'

import { refreshOnce } from '@/lib/refresh'
import {
  isAccessTokenExpired,
  needsRefresh,
} from '@/lib/refreshTiming'
import { safeNext } from '@/lib/safeNext'

function refreshMarginMs(): number {
  const raw = process.env.REFRESH_MARGIN_SECONDS

  if (raw === undefined) {
    return 60_000
  }

  const seconds = Number(raw)

  if (
    !Number.isFinite(seconds) ||
    seconds < 0 ||
    seconds > 3600
  ) {
    return 60_000
  }

  return seconds * 1000
}

function loginRedirect(
  request: NextRequest,
  expired = false,
): NextResponse {
  const url = new URL('/login', request.url)

  if (expired) {
    url.searchParams.set('expired', '1')
  }

  const requestedPath =
    request.nextUrl.pathname +
    request.nextUrl.search

  const destination = safeNext(requestedPath)

  if (destination) {
    url.searchParams.set('next', destination)
  }

  const status = request.method === 'GET' ? 307 : 303

  return NextResponse.redirect(url, status)
}

function clearSession(
  response: NextResponse,
): NextResponse {
  response.cookies.set(
    SESSION_COOKIE,
    '',
    {
      ...sessionCookieOptions(),
      maxAge: 0,
    },
  )

  return response
}

export async function proxy(
  request: NextRequest,
): Promise<NextResponse> {
  const encrypted =
    request.cookies.get(SESSION_COOKIE)?.value

  const session = await openSession(encrypted)

  if (!session) {
    const response = loginRedirect(request)

    if (encrypted) {
      return clearSession(response)
    }

    return response
  }

  if (
    !needsRefresh(
      session.accessTokenExpiresAtUtc,
      Date.now(),
      refreshMarginMs(),
    )
  ) {
    return NextResponse.next()
  }

  const outcome = await refreshOnce(session)

  if (outcome.kind === 'refused') {
    const response = loginRedirect(request, true)

    return clearSession(response)
  }

  if (outcome.kind === 'temporary') {
    if (
      !isAccessTokenExpired(
        session.accessTokenExpiresAtUtc,
      )
    ) {
      return NextResponse.next()
    }

    return new NextResponse(
      'RondiTrack cannot renew your session right now. Please try again.',
      {
        status: 503,
        headers: {
          'Content-Type':
            'text/plain; charset=utf-8',
          'Cache-Control': 'no-store',
        },
      },
    )
  }

  const sealed = await sealSession(
    outcome.session,
  )

  request.cookies.set(
    SESSION_COOKIE,
    sealed,
  )

  const requestHeaders = new Headers(
    request.headers,
  )

  const response = NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  })

  response.cookies.set(
    SESSION_COOKIE,
    sealed,
    sessionCookieOptions(),
  )

  return response
}

export const config = {
  matcher: [
    '/dashboard/:path*',
    '/stokvels/:path*',
  ],
}
