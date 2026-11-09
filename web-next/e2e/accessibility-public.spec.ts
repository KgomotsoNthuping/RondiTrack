import {
  test,
  expect,
} from '@playwright/test'

import { assertAccessible } from './accessibility-helpers'

test.use({
  storageState: {
    cookies: [],
    origins: [],
  },
})

test('sign-in form passes the accessibility scan', async ({
  page,
}) => {
  await page.goto('/login')

  await expect(
    page.getByRole('button', {
      name: 'Sign in',
    }),
  ).toBeVisible()

  await assertAccessible(page)
})

test('sign-in supports keyboard and paste', async ({
  page,
}) => {
  await page.goto('/login')

  const email = page.getByRole('textbox', {
    name: 'Email address',
  })

  const password = page.getByLabel('Password')

  await expect(email).toHaveAttribute(
    'autocomplete',
    'username',
  )

  await expect(password).toHaveAttribute(
    'autocomplete',
    'current-password',
  )

  await email.focus()

  await page.keyboard.insertText(
    'lerato@ronditrack.co.za',
  )

  await expect(email).toHaveValue(
    'lerato@ronditrack.co.za',
  )

  await password.focus()

  await page.keyboard.insertText(
    'example-test-password',
  )

  await expect(password).toHaveValue(
    'example-test-password',
  )
})
