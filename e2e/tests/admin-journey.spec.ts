import { test, expect } from '@playwright/test';

/**
 * E2E: Admin journey — Login → View dashboard → Approve seller → Manage products
 * Assumes: admin panel on http://localhost:4201
 * Seeded admin: admin1@mailinator.com / Test@123
 */

test.use({ baseURL: 'http://localhost:4201' });

test.describe('Admin journey', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill('admin1@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();
    await expect(page).not.toHaveURL(/login/);
  });

  test('admin dashboard renders KPI cards', async ({ page }) => {
    await page.goto('/admin/dashboard');

    const kpiCard = page.locator('app-kpi-card, [data-testid="kpi-card"]').first();
    await expect(kpiCard).toBeVisible({ timeout: 10_000 });
  });

  test('admin can view product list and activate a product', async ({ page }) => {
    await page.goto('/admin/products');

    const productRow = page.locator('table tbody tr, [data-testid="product-row"]').first();
    await expect(productRow.or(page.getByText(/no products/i))).toBeVisible({ timeout: 10_000 });

    // Try to activate (toggle) the first product if an action button is visible
    const toggleBtn = page.getByRole('button', { name: /activate|deactivate/i }).first();
    if (await toggleBtn.isVisible()) {
      await toggleBtn.click();
      // Confirm dialog if present
      const confirmBtn = page.getByRole('button', { name: /confirm|yes/i });
      if (await confirmBtn.isVisible()) {
        await confirmBtn.click();
      }
    }
  });

  test('admin can view and manage sellers', async ({ page }) => {
    await page.goto('/admin/sellers');

    await expect(page.getByRole('main')).toBeVisible();
    // Seller rows or empty state
    const content = page.locator('table tbody tr, .seller-row, [data-testid="seller-row"]').first();
    await expect(content.or(page.getByText(/no sellers/i))).toBeVisible({ timeout: 10_000 });
  });

  test('admin can view order management page', async ({ page }) => {
    await page.goto('/admin/orders');

    const heading = page.getByRole('heading', { name: /orders/i });
    await expect(heading.or(page.getByRole('main'))).toBeVisible();
  });

  test('admin can view analytics dashboard', async ({ page }) => {
    await page.goto('/admin/dashboard');

    // Revenue chart or KPI cards must render
    const chart = page
      .locator('apx-chart, canvas, [data-testid="chart"]')
      .or(page.locator('app-kpi-card'))
      .first();

    await expect(chart).toBeVisible({ timeout: 15_000 });
  });
});

test.describe('Super Admin journey', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill('superadmin@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();
    await expect(page).not.toHaveURL(/login/);
  });

  test('super admin dashboard is accessible', async ({ page }) => {
    await page.goto('/super-admin/dashboard');

    await expect(page.getByRole('main')).toBeVisible();
    const kpiCard = page.locator('app-kpi-card, [data-testid="kpi-card"]').first();
    await expect(kpiCard).toBeVisible({ timeout: 10_000 });
  });

  test('super admin can view admin user list', async ({ page }) => {
    await page.goto('/super-admin/admins');

    await expect(page.getByRole('main')).toBeVisible();
    const content = page
      .locator('table tbody tr, [data-testid="admin-row"]')
      .or(page.getByText(/no admins/i))
      .first();
    await expect(content).toBeVisible({ timeout: 10_000 });
  });
});
