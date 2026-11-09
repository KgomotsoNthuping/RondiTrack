import {
  test,
  expect,
} from './fixtures'

import { assertAccessible } from './accessibility-helpers'

test('member dashboard has no serious WCAG violations', async ({
  page,
}) => {
  await page.goto('/dashboard/member')

  await expect(
    page.getByRole('heading', {
      name: 'Member dashboard',
    }),
  ).toBeVisible()

  await assertAccessible(page)
})


test('keyboard users can skip directly to main content', async ({
  page,
}) => {
  await page.goto('/dashboard/member')

  await expect(
    page.getByRole('heading', {
      name: 'Member dashboard',
    }),
  ).toBeVisible()

  await page.keyboard.press('Tab')

  const skip = page.getByRole('link', {
    name: 'Skip to main content',
  })

  await expect(skip).toBeFocused()

  await page.keyboard.press('Enter')

  await expect(
    page.locator('#main-content'),
  ).toBeFocused()
})


test('joining wizard focuses the first step heading', async ({
  page,
}) => {
  await page.goto('/join')

  const heading = page.getByRole('heading', {
    name: /Step 1 of 3/,
  })

  await expect(heading).toBeFocused()

  await expect(
    page.locator('[aria-current="step"]'),
  ).toHaveCount(1)

  const codeInput = page.getByRole('textbox', {
    name: 'Invitation code',
  })

  await codeInput.focus()

  await page.keyboard.insertText(
    'a'.repeat(43),
  )

  await expect(codeInput).toHaveValue(
    'a'.repeat(43),
  )
})


test('stokvel list has no serious WCAG violations', async ({
  page,
}) => {
  await page.goto('/stokvels')

  await expect(
    page.getByRole('heading', {
      name: 'My stokvels',
    }),
  ).toBeVisible()

  await assertAccessible(page)
})

test('joining code step is accessible', async ({
  page,
}) => {
  await page.goto('/join')

  await expect(
    page.getByRole('heading', {
      name: /Step 1 of 3/,
    }),
  ).toBeVisible()

  await expect(
    page.getByRole('textbox', {
      name: 'Invitation code',
    }),
  ).toBeVisible()

  await assertAccessible(page)
})

test('stokvel contribution form is accessible', async ({
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
      name: 'Record contribution',
    }),
  ).toBeVisible()

  await assertAccessible(page)
})

test('contribution form errors remain accessible', async ({
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

  const amount = page.getByRole('textbox', {
    name: 'Amount (rands)',
  })

  await amount.fill('0')

  await page.getByRole('button', {
    name: 'Record contribution',
  }).click()

  await expect(
    page.getByRole('alert').first(),
  ).toBeVisible()

  await expect(amount).toHaveAttribute(
    'aria-invalid',
    'true',
  )

  await assertAccessible(page)
})
