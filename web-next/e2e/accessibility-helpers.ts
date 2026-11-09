import AxeBuilder from '@axe-core/playwright'
import {
  expect,
  type Page,
} from '@playwright/test'

export const wcagTags = [
  'wcag2a',
  'wcag2aa',
  'wcag21a',
  'wcag21aa',
  'wcag22aa',
]

export async function assertAccessible(
  page: Page,
): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(wcagTags)
    .analyze()

  const serious = results.violations.filter(
    violation =>
      violation.impact === 'serious' ||
      violation.impact === 'critical',
  )

  expect(
    serious.map(violation => ({
      id: violation.id,
      impact: violation.impact,
      description: violation.description,
      affected: violation.nodes.map(node => ({
        target: node.target,
        summary: node.failureSummary,
      })),
    })),
  ).toEqual([])
}
