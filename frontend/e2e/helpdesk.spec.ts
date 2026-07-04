import { expect, test, type Page } from '@playwright/test'

const password = 'Password123!'

async function loginAs(page: Page, email: string) {
  await page.goto('/')
  await page.getByTestId('login-email').fill(email)
  await page.getByTestId('login-password').fill(password)
  await page.getByTestId('login-submit').click()
  await expect(page.getByTestId('dashboard-view')).toBeVisible({ timeout: 15000 })
}

async function openTickets(page: Page) {
  await page.getByTestId('nav-tickets').click()
  await expect(page.getByTestId('tickets-view')).toBeVisible()
}

async function openAssets(page: Page) {
  await page.getByTestId('nav-assets').click()
  await expect(page.getByTestId('assets-view')).toBeVisible()
}

async function openUsers(page: Page) {
  await page.getByTestId('nav-users').click()
  await expect(page.getByTestId('users-view')).toBeVisible()
}

async function openCategories(page: Page) {
  await page.getByTestId('nav-categories').click()
  await expect(page.getByTestId('categories-view')).toBeVisible()
}

async function openAudit(page: Page) {
  await page.getByTestId('nav-audit').click()
  await expect(page.getByTestId('audit-view')).toBeVisible()
}

function ticketRowByTitle(page: Page, title: string | RegExp) {
  return page.getByRole('button', { name: title })
}

test('language switch localizes operational labels', async ({ page }) => {
  await page.goto('/')
  await page.locator('.language-toggle').getByRole('button', { name: 'PL', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Logowanie' })).toBeVisible()

  await page.getByTestId('login-email').fill('admin@test.com')
  await page.getByTestId('login-password').fill(password)
  await page.getByTestId('login-submit').click()
  await expect(page.getByTestId('dashboard-view')).toBeVisible({ timeout: 15000 })
  await expect(page).toHaveURL(/\/dashboard$/)

  await expect(page.getByTestId('nav-tickets')).toContainText('Zgłoszenia')
  await expect(page.getByTestId('nav-assets')).toContainText('Sprzęt')

  await openTickets(page)
  await expect(page).toHaveURL(/\/tickets$/)
  await expect(page.getByTestId('filter-category')).toContainText('Sprzęt')
  await expect(page.getByTestId('filter-tag')).toContainText('Czeka na dostawcę')
  const laptopTicket = ticketRowByTitle(page, /Laptop overheats during video calls/)
  await expect(laptopTicket).toContainText('Sprzęt / Ewa Nowak')
  await laptopTicket.click()
  await expect(page.getByText('Zmieniono status').first()).toBeVisible()

  await page.getByTestId('nav-outbox').click()
  await expect(page.getByTestId('notifications-view')).toBeVisible()
  await expect(page.getByTestId('notifications-view')).toContainText(/wiadomości|Brak wiadomości w kolejce/)
})

test('employee logs in and creates a new ticket', async ({ page }) => {
  await loginAs(page, 'employee@test.com')
  const initialOpen = Number(await page.getByTestId('metric-open-tickets').locator('strong').innerText())

  await openTickets(page)
  await page.getByTestId('new-ticket-toggle').click()
  await page.getByTestId('new-ticket-title').fill('E2E payroll laptop dock issue')
  await page.getByTestId('new-ticket-description').fill('Docking station disconnects external displays during payroll review.')
  await page.getByTestId('new-ticket-category').selectOption({ label: 'Hardware' })
  await page.getByTestId('new-ticket-priority').selectOption('High')
  await page.getByTestId('new-ticket-submit').click()

  await expect(page.getByRole('heading', { name: 'E2E payroll laptop dock issue' })).toBeVisible()
  await page.getByTestId('nav-dashboard').click()
  await expect(page.getByTestId('metric-open-tickets').locator('strong')).toHaveText(String(initialOpen + 1))
})

test('technician assigns a ticket, starts work and resolves it', async ({ page }) => {
  await loginAs(page, 'technician@test.com')
  await openTickets(page)
  await ticketRowByTitle(page, /Meeting room Wi-Fi drops calls/).click()

  await page.getByTestId('assign-to-me').click()
  await expect(page.getByTestId('selected-ticket-status')).toContainText('Assigned')

  await page.getByTestId('work-ticket').click()
  await expect(page.getByTestId('selected-ticket-status')).toContainText('In progress')

  await page.getByTestId('resolve-ticket').click()
  await expect(page.getByTestId('selected-ticket-status')).toContainText('Resolved')
})

test('technician adds public and internal comments', async ({ page }) => {
  await loginAs(page, 'technician@test.com')
  await openTickets(page)
  await ticketRowByTitle(page, /Laptop overheats during video calls/).click()

  await page.getByTestId('comment-body').fill('Public E2E update: firmware check finished.')
  await page.getByTestId('comment-submit').click()
  await expect(page.getByText('Public E2E update: firmware check finished.')).toBeVisible()

  await page.getByTestId('comment-body').fill('Internal E2E note: prepare replacement device.')
  await page.getByTestId('comment-internal').check()
  await page.getByTestId('comment-submit').click()
  await expect(page.getByText('Internal E2E note: prepare replacement device.')).toBeVisible()

  await page.getByTestId('attachment-upload').setInputFiles({
    name: 'diagnostics.txt',
    mimeType: 'text/plain',
    buffer: Buffer.from('Fan curve and firmware diagnostics attached by E2E.'),
  })
  await expect(page.getByTestId('attachment-row-diagnostics.txt')).toContainText('diagnostics.txt')
  const downloadPromise = page.waitForEvent('download')
  await page.getByTestId('attachment-download-diagnostics.txt').click()
  const download = await downloadPromise
  expect(download.suggestedFilename()).toBe('diagnostics.txt')
})

test('employee cannot see internal comments in ticket details', async ({ page }) => {
  await loginAs(page, 'employee@test.com')
  await openTickets(page)
  await ticketRowByTitle(page, /Laptop overheats during video calls/).click()

  await expect(page.getByText('It happens mostly when I share the screen.')).toBeVisible()
  await expect(page.getByText('Thermal diagnostics started. Checking firmware and fan curve.')).toBeHidden()
})

test('admin creates an asset and assigns it to an employee', async ({ page }) => {
  await loginAs(page, 'admin@test.com')
  await openAssets(page)

  await page.getByTestId('asset-import-csv').setInputFiles({
    name: 'assets.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from('AssetTag,Type,Brand,Model,SerialNumber,PurchaseDate,WarrantyUntil,Notes\nCSV-LTP-001,Laptop,Dell,Latitude 7450,CSV-SN-001,2026-07-03,2029-07-03,Imported by Playwright'),
  })
  await expect(page.getByTestId('asset-card-CSV-LTP-001')).toContainText('Dell Latitude 7450')

  const downloadPromise = page.waitForEvent('download')
  await page.getByTestId('asset-export-csv').click()
  const download = await downloadPromise
  expect(download.suggestedFilename()).toBe('helpdesk-assets.csv')

  await page.getByTestId('asset-tag').fill('E2E-LTP-001')
  await page.getByTestId('asset-type').selectOption('Laptop')
  await page.getByTestId('asset-brand').fill('Framework')
  await page.getByTestId('asset-model').fill('Laptop 13')
  await page.getByTestId('asset-serial-number').fill('E2E-SN-001')
  await page.getByTestId('asset-purchase-date').fill('2026-07-03')
  await page.getByTestId('asset-warranty-until').fill('2029-07-03')
  await page.getByTestId('asset-notes').fill('Created by Playwright E2E')
  await page.getByTestId('asset-submit').click()

  const assetCard = page.getByTestId('asset-card-E2E-LTP-001')
  await expect(assetCard).toContainText('Framework Laptop 13')
  await page.getByTestId('asset-assign-E2E-LTP-001').selectOption({ label: 'Ewa Nowak' })
  await expect(assetCard).toContainText('Ewa Nowak')
  await expect(page.getByTestId('asset-details-panel')).toContainText('Framework Laptop 13')
  await expect(page.getByTestId('asset-assignment-history')).toContainText('Ewa Nowak')
  await expect(page.getByTestId('asset-assignment-history')).toContainText('Active')

  await openAudit(page)
  await page.getByTestId('audit-entity-filter').selectOption('Asset')
  await expect(page.getByTestId('audit-view')).toContainText('Asset created')
  await expect(page.getByTestId('audit-view')).toContainText('E2E-LTP-001')
  await page.getByTestId('audit-search').fill('assigned')
  await expect(page.getByTestId('audit-view')).toContainText('Asset assigned')
})

test('technician links an asset to a ticket', async ({ page }) => {
  await loginAs(page, 'technician@test.com')
  await openTickets(page)
  await ticketRowByTitle(page, /Laptop overheats during video calls/).click()

  await page.getByTestId('ticket-asset-select').selectOption({ label: 'MON-2026-014' })
  await expect(page.getByTestId('selected-ticket-asset')).toHaveText('MON-2026-014')
})

test('admin creates a user and changes their role', async ({ page }) => {
  await loginAs(page, 'admin@test.com')
  await openUsers(page)

  await page.getByTestId('user-full-name').fill('E2E Support Analyst')
  await page.getByTestId('user-email').fill('e2e.support@test.com')
  await page.getByTestId('user-password').fill(password)
  await page.getByTestId('user-role').selectOption('Employee')
  await page.getByTestId('user-department').fill('IT Support')
  await page.getByTestId('user-submit').click()

  const userRow = page.getByTestId('user-row-e2e.support@test.com')
  await expect(userRow).toContainText('E2E Support Analyst')
  await page.getByTestId('user-role-e2e.support@test.com').selectOption('Technician')
  await expect(page.getByTestId('user-role-e2e.support@test.com')).toHaveValue('Technician')
})

test('admin creates and deactivates a ticket category', async ({ page }) => {
  await loginAs(page, 'admin@test.com')
  await openCategories(page)

  await page.getByTestId('category-name').fill('Security')
  await page.getByTestId('category-description').fill('Identity, device hardening and security access requests.')
  await page.getByTestId('category-submit').click()

  const categoryRow = page.getByTestId('category-row-Security')
  await expect(page.getByTestId('category-description-Security')).toHaveValue('Identity, device hardening and security access requests.')
  await page.getByTestId('category-active-Security').uncheck()
  await page.getByTestId('category-save-Security').click()
  await expect(categoryRow).toContainText('Inactive')
})
