export function shouldHideStokvel(
  status: number,
): boolean {
  return status === 403 || status === 404
}
