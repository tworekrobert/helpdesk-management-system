import { defineConfig, devices } from '@playwright/test'

const frontendPort = process.env.PLAYWRIGHT_PORT ?? '5175'
const frontendUrl = `http://127.0.0.1:${frontendPort}`
const apiUrl = process.env.VITE_API_URL ?? 'http://127.0.0.1:59999'

export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  fullyParallel: true,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: frontendUrl,
    trace: 'on-first-retry',
  },
  webServer: {
    command: `npm run dev -- --host 127.0.0.1 --port ${frontendPort}`,
    env: {
      VITE_API_URL: apiUrl,
    },
    reuseExistingServer: true,
    timeout: 120_000,
    url: frontendUrl,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
