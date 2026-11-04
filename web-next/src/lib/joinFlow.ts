import type { InvitePreview } from './inviteTypes'

export type JoinStep = 'code' | 'rules' | 'confirm'

export type JoinState =
  | {
      step: 'code'
      code: string
    }
  | {
      step: 'rules'
      code: string
      preview: InvitePreview
    }
  | {
      step: 'confirm'
      code: string
      preview: InvitePreview
    }

export type JoinEvent =
  | {
      type: 'CODE_ACCEPTED'
      preview: InvitePreview
    }
  | {
      type: 'CONTINUE'
    }
  | {
      type: 'BACK'
    }

export function isInviteCode(value: string): boolean {
  return /^[A-Za-z0-9_-]{43}$/.test(value)
}

export function parseJoinStep(
  value: unknown,
): JoinStep | null {
  if (
    value === 'code' ||
    value === 'rules' ||
    value === 'confirm'
  ) {
    return value
  }

  if (value === undefined || value === null) {
    return 'code'
  }

  return null
}

export function joinHref(
  code: string,
  step: JoinStep,
): string {
  const query = new URLSearchParams()

  if (code) {
    query.set('code', code)
  }

  if (step !== 'code') {
    query.set('step', step)
  }

  const suffix = query.toString()

  return suffix ? `/join?${suffix}` : '/join'
}

export function transitionJoin(
  state: JoinState,
  event: JoinEvent,
): JoinState {
  if (event.type === 'CODE_ACCEPTED') {
    if (
      state.step !== 'code' ||
      !isInviteCode(state.code) ||
      event.preview.alreadyMember
    ) {
      return state
    }

    return {
      step: 'rules',
      code: state.code,
      preview: event.preview,
    }
  }

  if (event.type === 'CONTINUE') {
    if (state.step !== 'rules') {
      return state
    }

    return {
      ...state,
      step: 'confirm',
    }
  }

  if (state.step === 'confirm') {
    return {
      ...state,
      step: 'rules',
    }
  }

  if (state.step === 'rules') {
    return {
      step: 'code',
      code: state.code,
    }
  }

  return state
}
