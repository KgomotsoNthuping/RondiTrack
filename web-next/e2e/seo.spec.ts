import {
  request as playwrightRequest,
} from '@playwright/test'
import {
  test,
  expect,
} from './fixtures'

test('signed-in dashboard is noindex', async ({
  page,
}) => {
  await page.goto('/dashboard/member')

  await expect(
    page.getByRole('heading', {
      name: 'Member dashboard',
    }),
  ).toBeVisible()

  const robots = page.locator('meta[name="robots"]')

  await expect(robots).toHaveAttribute(
    'content',
    /noindex/,
  )
})

test('stokvel details are noindex', async ({
  page,
}) => {
  await page.goto('/stokvels')

  const work = page
    .getByRole('article')
    .filter({
      has: page.getByRole('heading', {
        name: 'Work Savers',
      }),
    })

  await work.getByRole('link', {
    name: 'View stokvel',
  }).click()

  await expect(
    page.getByRole('heading', {
      name: 'Work Savers',
    }),
  ).toBeVisible()

  await expect(
    page.locator('meta[name="robots"]'),
  ).toHaveAttribute('content', /noindex/)
})

test('BFF routes return an X-Robots-Tag header', async ({
  request,
}) => {
  const response = await request.get('/bff/activity')

  expect(
    response.headers()['x-robots-tag'],
  ).toContain('noindex')

  expect(
    response.headers()['cache-control'],
  ).toContain('no-store')
})

test('robots.txt does not block reading noindex', async ({
  request,
}) => {
  const response = await request.get('/robots.txt')

  expect(response.ok()).toBe(true)

  const contents = await response.text()

  expect(contents).toMatch(/User-Agent:\s*\*/i)
  expect(contents).toMatch(/Allow:\s*\//i)
  expect(contents).not.toMatch(/Disallow:\s*\/stokvels/i)
  expect(contents).not.toMatch(/Disallow:\s*\/dashboard/i)
})

test('a real invitation does not leak names or amounts to a WhatsApp bot', async ({
  page,
}) => {
  await page.goto('/stokvels')

  const family = page
    .getByRole('article')
    .filter({
      has: page.getByRole('heading', {
        name: 'Family Savers',
      }),
    })

  await family.getByRole('link', {
    name: 'View stokvel',
  }).click()

  await page.getByRole('button', {
    name: 'Create invitation link',
  }).click()

  const invitationField = page.getByRole('textbox', {
    name: 'Invitation link',
  })

  await expect(invitationField).toBeVisible()

  const invitationPath = await invitationField.inputValue()

  const invitation = new URL(
    invitationPath,
    'http://localhost:3000',
  )

  const code = invitation.searchParams.get('code')

  expect(code).toBeTruthy()

  const bot = await playwrightRequest.newContext({
    baseURL: 'http://localhost:3000',
    extraHTTPHeaders: {
      'User-Agent': 'WhatsApp/2',
    },
    storageState: {
      cookies: [],
      origins: [],
    },
  })

  try {
    const redirectResponse = await bot.get(
      invitation.pathname + invitation.search,
      { maxRedirects: 0 },
    )

    expect(redirectResponse.status()).toBe(307)

    const destination =
      redirectResponse.headers()['location']

    expect(destination).toContain('/login')

    const response = await bot.get(
      invitation.pathname + invitation.search,
    )

    expect(response.ok()).toBe(true)

    const html = await response.text()
    const head = html.match(
      /<head\b[^>]*>([\s\S]*?)<\/head>/i,
    )?.[1]

    expect(head).toBeDefined()
    expect(head).toMatch(/RondiTrack/i)
    expect(head).toMatch(/og:title/i)
    expect(head).toMatch(/og:description/i)

    expect(head).not.toContain('Family Savers')
    expect(head).not.toContain('Work Savers')
    expect(head).not.toContain('Lerato')
    expect(head).not.toContain(code!)
    expect(head).not.toMatch(/R\s*500(?:[.,]00)?/i)
  } finally {
    await bot.dispose()
  }
})
