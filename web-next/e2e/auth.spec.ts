import {
  expect,
  test,
} from '@playwright/test'

test.use({
  storageState: {
    cookies: [],
    origins: [],
  },
})

test('signed-out user returns to login with the requested page', async ({
  page,
}) => {
  await page.goto('/stokvels')

  await expect(page).toHaveURL(
    /\/login\?/,
  )

  const url = new URL(page.url())

  expect(url.searchParams.get('next')).toBe(
    '/stokvels',
  )

  await expect(
    page.getByRole('button', {
      name: 'Sign in',
    }),
  ).toBeVisible()
})

test('signed-out BFF request returns JSON 401', async ({
  request,
}) => {
  const response = await request.get(
    '/bff/activity',
  )

  expect(response.status()).toBe(401)

  expect(
    response.headers()['content-type'],
  ).toContain('application/json')

  const body = await response.json()

  expect(body.status).toBe(401)

  expect(body.title).toMatch(
    /session|sign in|unauthorized/i,
  )
})

test('signed-out invitation keeps the code in the return URL', async ({
  page,
}) => {
  const code = 'a'.repeat(43)

  await page.goto(
    `/join?code=${code}&step=rules`,
  )

  await expect(page).toHaveURL(
    /\/login\?/,
  )

  const url = new URL(page.url())
  const destination = url.searchParams.get('next')

  expect(destination).toBe(
    `/join?code=${code}&step=rules`,
  )
})
