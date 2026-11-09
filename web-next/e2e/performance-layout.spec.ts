import {
  test,
  expect,
} from './fixtures'

test.use({
  viewport: {
    width: 375,
    height: 812,
  },
})

test('member dashboard fits a mobile viewport', async ({
  page,
}) => {
  await page.goto('/dashboard/member')

  await expect(
    page.getByRole('heading', {
      name: 'Member dashboard',
    }),
  ).toBeVisible()

  const dimensions = await page.evaluate(() => ({
    viewportWidth: document.documentElement.clientWidth,
    documentWidth: document.documentElement.scrollWidth,
  }))

  expect(dimensions.documentWidth).toBeLessThanOrEqual(
    dimensions.viewportWidth,
  )

  await expect(
    page.getByRole('heading', {
      name: 'My memberships',
    }),
  ).toBeVisible()
})

test('stokvel details fit a mobile viewport', async ({
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

  const dimensions = await page.evaluate(() => ({
    viewportWidth: document.documentElement.clientWidth,
    documentWidth: document.documentElement.scrollWidth,
  }))

  expect(dimensions.documentWidth).toBeLessThanOrEqual(
    dimensions.viewportWidth,
  )
})
