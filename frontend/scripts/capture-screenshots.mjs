import { chromium } from 'playwright'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDir = path.dirname(fileURLToPath(import.meta.url))
const repoRoot = path.resolve(scriptDir, '..', '..')
const outputDir = path.resolve(repoRoot, 'docs', 'screenshots')
const baseUrl = process.env.SCREENSHOT_BASE_URL ?? 'http://127.0.0.1:5176'

async function assertNoDemoBanner(page) {
  const banner = page.getByText('API is not running. Demo data is active.')
  if ((await banner.count()) > 0 && (await banner.first().isVisible())) {
    throw new Error('Screenshot capture stopped because the frontend is running in demo mode.')
  }
}

async function capture(page, fileName) {
  await page.mouse.move(1590, 950)
  await page.evaluate(() => {
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur()
    }
  })
  await assertNoDemoBanner(page)
  await page.screenshot({
    path: path.join(outputDir, fileName),
    fullPage: false,
  })
}

await fs.mkdir(outputDir, { recursive: true })

const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: 1600, height: 960 } })

try {
  await page.goto(baseUrl, { waitUntil: 'networkidle' })
  await capture(page, 'login.png')

  await page.getByTestId('login-email').fill('admin@test.com')
  await page.getByTestId('login-password').fill('Password123!')
  await page.getByTestId('login-submit').click()
  await page.getByTestId('dashboard-view').waitFor()
  await capture(page, 'dashboard.png')

  const targets = [
    ['nav-tickets', 'tickets-view', 'tickets.png'],
    ['nav-assets', 'assets-view', 'assets.png'],
    ['nav-users', 'users-view', 'users.png'],
    ['nav-categories', 'categories-view', 'categories.png'],
    ['nav-audit', 'audit-view', 'audit.png'],
  ]

  for (const [navTestId, viewTestId, fileName] of targets) {
    await page.getByTestId(navTestId).click()
    await page.getByTestId(viewTestId).waitFor()
    await capture(page, fileName)
  }
} finally {
  await browser.close()
}
