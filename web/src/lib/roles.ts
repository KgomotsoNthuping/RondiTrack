
export function isTreasurerRole(
  role: string | null | undefined,
): boolean {
  return role === 'Treasurer'
}

export function roleLabel(
  role: string | null | undefined,
): string {
  switch (role) {
    case 'Treasurer':
      return "You're the Treasurer"

    case 'Member':
      return "You're a Member"

    case 'Admin':
      return "You're an Admin"

    default:
      return role
        ? `Role: ${role}`
        : 'Role not assigned'
  }
}
