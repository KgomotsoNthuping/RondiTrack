export const queryKeys = {
  me: ['me'] as const,

  home: ['me', 'home'] as const,

  stokvels: ['stokvels'] as const,

  stokvel: (stokvelId: string) =>
    ['stokvels', stokvelId] as const,

  cycles: (stokvelId: string) =>
    ['stokvels', stokvelId, 'cycles'] as const,

  contributions: (stokvelId: string) =>
    ['stokvels', stokvelId, 'contributions'] as const,
}
