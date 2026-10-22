import { ApiError } from '../api/ApiError'

const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5227'
).replace(/\/$/, '')

const AUTH_URL = `${API_BASE_URL}/api/v1/auth`

type AuthResponse = {
  accessToken: string
  accessTokenExpiresAtUtc: string
}

let accessToken: string | null = null
let refreshPromise: Promise<string | null> | null = null
let sessionVersion = 0
let signedOut = true

type SignedOutListener = () => void

const signedOutListeners = new Set<SignedOutListener>()

function announceSignedOut() {
  if (signedOut) return

  signedOut = true
  for (const listener of signedOutListeners) {
    listener()
  }
}

function clearSession() {
  sessionVersion++
  accessToken = null
  announceSignedOut()
}

export function onSignedOut(listener: SignedOutListener) {
  signedOutListeners.add(listener)

  return () => {
    signedOutListeners.delete(listener)
  }
}

export async function login(
  email: string,
  password: string,
): Promise<void> {
  const version = ++sessionVersion
  accessToken = null

  let response: Response

  try {
    response = await fetch(`${AUTH_URL}/login`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    })
  } catch {
    clearSession()
    throw new ApiError(0, {
      title: "Can't reach RondiTrack",
    })
  }

  if (!response.ok) {
    clearSession()
    throw await ApiError.fromResponse(response)
  }

  const data = (await response.json()) as AuthResponse

  if (version !== sessionVersion) {
    return
  }

  if (!data.accessToken) {
    clearSession()
    throw new ApiError(response.status, {
      title: 'Missing access token',
    })
  }

  accessToken = data.accessToken
  signedOut = false
}

async function doRefresh(): Promise<string | null> {
  const version = sessionVersion

  try {
    const response = await fetch(`${AUTH_URL}/refresh`, {
      method: 'POST',
      credentials: 'include',
    })

    if (version !== sessionVersion) {
      return null
    }

    if (!response.ok) {
      clearSession()
      return null
    }

    const data = (await response.json()) as AuthResponse

    if (!data.accessToken) {
      clearSession()
      return null
    }

    accessToken = data.accessToken
    signedOut = false

    return accessToken
  } catch {
    if (version === sessionVersion) {
      clearSession()
    }
    return null
  }
}

export function refreshAccessToken(): Promise<string | null> {
  if (refreshPromise !== null) {
    return refreshPromise
  }

  const current = doRefresh()
  refreshPromise = current

  void current.finally(() => {
    if (refreshPromise === current) {
      refreshPromise = null
    }
  })

  return current
}

export async function logout(): Promise<void> {
  clearSession()


  try {
    await fetch(`${AUTH_URL}/logout`, {
      method: 'POST',
      credentials: 'include',
    })
  } catch {
  }
}

export async function authFetch(
  input: RequestInfo | URL,
  init?: RequestInit,
): Promise<Response> {
  const request = new Request(input, {
    ...init,
    credentials: 'include',
  })

  const retryCopy = request.clone()

  let token = accessToken

  if (token === null) {
    token = await refreshAccessToken()
  }

  const firstRequest = request.clone()

  if (token !== null) {
    firstRequest.headers.set('Authorization', `Bearer ${token}`)
  }

  const firstResponse = await fetch(firstRequest)

  if (firstResponse.status !== 401) {
    return firstResponse
  }

  if (token === null) {
    return firstResponse
  }

  let replacement = accessToken

  if (replacement === token) {
    replacement = await refreshAccessToken()
  }

  if (replacement === null) {
    return firstResponse
  }

  retryCopy.headers.set(
    'Authorization',
    `Bearer ${replacement}`,
  )

  const retryResponse = await fetch(retryCopy)

  if (retryResponse.status === 401) {
    clearSession()
  }

  return retryResponse
}
