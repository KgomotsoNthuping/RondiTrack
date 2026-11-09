import {
  expect,
  test,
} from './fixtures'

import {
  parseRandsToCents,
} from '../src/lib/money'

function readCents(value: string): number {
  const result = parseRandsToCents(value)

  if (result === null) {
    throw new Error(
      `Could not parse displayed money: ${value}`,
    )
  }

  return result
}

test('recording R150,50 increases the displayed total by exactly 15050 cents', async ({
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

  const collectedCard = page.getByText(
    'Total collected',
    { exact: true },
  ).locator('..')

  const amountDisplay = collectedCard.locator(
    'p',
  ).last()

  const startingText =
    await amountDisplay.innerText()

  const startingCents = readCents(
    startingText,
  )

  const form = page.getByRole('region', {
    name: 'Record contribution',
  })

  await expect(form).toBeVisible()

  await form.getByRole('textbox', {
    name: 'Amount (rands)',
  }).fill('150,50')

  await form.getByRole('button', {
    name: 'Record contribution',
  }).click()

  await expect(
    form.getByRole('status'),
  ).toContainText(
    'recorded successfully',
  )

  await expect.poll(async () => {
    const latestText =
      await amountDisplay.innerText()

    return (
      readCents(latestText) -
      startingCents
    )
  }).toBe(15050)
})
