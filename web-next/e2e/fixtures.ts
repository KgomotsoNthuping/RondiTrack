import {
  test as base,
  expect,
} from '@playwright/test'
import { fileURLToPath } from 'node:url'

const authFile = fileURLToPath(
  new URL('./.auth/lerato.json', import.meta.url),
)

export const test = base.extend({
  page: async ({ page }, use) => {
    await use(page)

    await page.context().storageState({
      path: authFile,
    })
  },
})

export { expect }
