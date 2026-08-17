import { test, expect } from '@playwright/test';

/**
 * E2E: Seller journey — Login → Create product → View orders
 * Assumes: admin panel on http://localhost:4201
 * Seeded seller: seller01@mailinator.com / Test@123
 */

test.use({ baseURL: 'http://localhost:4201' });

test.describe('Seller journey', () => {
  test.beforeEach(async ({ page }) => {
    // Log in as seller
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill('seller01@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();
    await expect(page).not.toHaveURL(/login/);
  });

  test('seller dashboard shows KPI cards', async ({ page }) => {
    await page.goto('/seller/dashboard');

    // KPI cards should be visible (products, orders, revenue)
    const kpiCard = page.locator('app-kpi-card, [data-testid="kpi-card"]').first();
    await expect(kpiCard).toBeVisible();
  });

  test('seller can create a new product', async ({ page }) => {
    await page.goto('/seller/products/create');
    await expect(page.getByRole('main')).toBeVisible();

    await page.getByLabel(/product name/i).fill(`E2E Test Dress ${Date.now()}`);
    await page.getByLabel(/base price/i).fill('1200');
    await page.getByLabel(/description/i).fill('Created by E2E test');

    // Select category (first option)
    const categorySelect = page.getByLabel(/category/i);
    await categorySelect.selectOption({ index: 1 });

    // Select brand (first option)
    const brandSelect = page.getByLabel(/brand/i);
    await brandSelect.selectOption({ index: 1 });

    // Add a variant row
    const addVariantBtn = page.getByRole('button', { name: /add variant/i });
    if (await addVariantBtn.isVisible()) {
      await addVariantBtn.click();
      await page.getByLabel(/sku/i).first().fill('E2E-SKU-001');
      await page.getByLabel(/size/i).first().fill('M');
      await page.getByLabel(/stock/i).first().fill('10');
    }

    await page.getByRole('button', { name: /save|create|submit/i }).click();

    // Should redirect to product list or show success
    await expect(
      page.getByText(/created|saved|success/i).or(page).not.toHaveURL(/create$/)
    ).toBeTruthy();
  });

  test('seller can view their orders', async ({ page }) => {
    await page.goto('/seller/orders');

    const heading = page.getByRole('heading', { name: /orders/i });
    await expect(heading.or(page.getByRole('main'))).toBeVisible();
  });

  test('seller can view inventory', async ({ page }) => {
    await page.goto('/seller/inventory');

    await expect(page.getByRole('main')).toBeVisible();
    const table = page.locator('table, [data-testid="inventory-table"]');
    await expect(table.or(page.getByText(/no inventory/i))).toBeVisible();
  });
});
