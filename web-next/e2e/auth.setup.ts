import {
  expect,
  test as setup,
} from '@playwright/test'
import { existsSync, mkdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const authFile = fileURLToPath(
  new URL('./.auth/lerato.json', import.meta.url),
)

setup('authenticate the demo member', async ({
  browser,
  page,
}) => {
  mkdirSync(
    fileURLToPath(
      new URL('./.auth/', import.meta.url),
    ),
    { recursive: true },
  )

  if (existsSync(authFile)) {
    const saved = await browser.newContext({
      storageState: authFile,
    })

    try {
      const checkPage = await saved.newPage()

      await checkPage.goto('http://localhost:3000/dashboard/member',)

      if (
        await checkPage
          .getByRole('heading', {
            name: 'Member dashboard',
          })
          .isVisible()
      ) {
        await saved.storageState({
          path: authFile,
        })

        return
      }
    } catch {
    } finally {
      await saved.close()
    }
  }

  const password = process.env.E2E_DEMO_PASSWORD

  if (!password) {
    throw new Error(
      'Set E2E_DEMO_PASSWORD to the local Compose DEMO_PASSWORD.',
    )
  }

  const email =
    process.env.E2E_DEMO_EMAIL ??
    'lerato@ronditrack.co.za'

  await page.goto('/login')

  await page.getByRole('textbox', {
    name: 'Email address',
  }).fill(email)

  await page.getByLabel('Password').fill(password)

  await page.getByRole('button', {
    name: 'Sign in',
  }).click()

  await expect(page).toHaveURL(
    /\/dashboard\/member(?:[/?#]|$)/,
  )

  await expect(
    page.getByRole('heading', {
      name: 'Member dashboard',
    }),
  ).toBeVisible()

  await page.context().storageState({
    path: authFile,
  })
})
