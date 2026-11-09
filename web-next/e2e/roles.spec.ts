import {
  expect,
  test,
} from './fixtures'

test('Lerato has different permissions in different stokvels', async ({
  page,
}) => {
  await page.goto('/stokvels')

  await expect(
    page.getByRole('heading', {
      name: 'My stokvels',
    }),
  ).toBeVisible()

  const family = page
    .getByRole('article')
    .filter({
      has: page.getByRole('heading', {
        name: 'Family Savers',
      }),
    })

  const work = page
    .getByRole('article')
    .filter({
      has: page.getByRole('heading', {
        name: 'Work Savers',
      }),
    })

  await expect(family).toContainText('Treasurer')
  await expect(work).toContainText('Member')

  await family.getByRole('link', {
    name: 'View stokvel',
  }).click()

  await expect(
    page.getByRole('heading', {
      name: 'Family Savers',
    }),
  ).toBeVisible()

  await expect(
    page.getByRole('heading', {
      name: 'Invite a member',
    }),
  ).toBeVisible()

  await page.goto('/stokvels')

  const workAgain = page
    .getByRole('article')
    .filter({
      has: page.getByRole('heading', {
        name: 'Work Savers',
      }),
    })

  await workAgain.getByRole('link', {
    name: 'View stokvel',
  }).click()

  await expect(
    page.getByRole('heading', {
      name: 'Work Savers',
    }),
  ).toBeVisible()

  await expect(
    page.getByRole('heading', {
      name: 'Invite a member',
    }),
  ).toHaveCount(0)

  await expect(
    page.getByRole('combobox', {
      name: 'Member',
    }),
  ).toHaveCount(0)
})
