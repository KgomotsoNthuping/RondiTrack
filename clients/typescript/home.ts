import type { components } from "./generated";

type MemberHome =
  components["schemas"]["MemberHomeResponse"];

export function totalStillOwedInCents(
  home: MemberHome,
): number {
  return (home.stokvels ?? []).reduce(
    (total, stokvel) =>
      total +
      (stokvel.stillOwedInCents ?? 0),
    0,
  );
}