import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  reporter: 'html',

  use: {
    baseURL:       'http://localhost:4200',
    trace:         'on-first-retry',
    screenshot:    'only-on-failure',
    actionTimeout: 15_000,
  },

  projects: [
    {
      name: 'chromium',
      use:  { ...devices['Desktop Chrome'] },
    },
  ],

  // Run storefront dev server if not already running (docker / npm start)
  // webServer: {
  //   command: 'npm start --prefix ../user-storefront',
  //   url:     'http://localhost:4200',
  //   reuseExistingServer: !process.env['CI'],
  // },
});
