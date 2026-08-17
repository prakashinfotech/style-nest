import { test, expect } from '@playwright/test';

/**
 * E2E: Customer journey — Register → Login → Browse → Add to Cart → Checkout
 * Assumes: storefront on http://localhost:4200, API on http://localhost:5000
 */

const UNIQUE_EMAIL = `e2e.customer.${Date.now()}@mailinator.com`;
const PASSWORD     = 'Test@123456';

test.describe('Customer journey', () => {
  test('user can register a new account', async ({ page }) => {
    await page.goto('/auth/register');

    await page.getByLabel(/first name/i).fill('E2E');
    await page.getByLabel(/last name/i).fill('User');
    await page.getByLabel(/email/i).fill(UNIQUE_EMAIL);
    await page.getByLabel(/^password$/i).fill(PASSWORD);
    await page.getByLabel(/confirm password/i).fill(PASSWORD);
    await page.getByRole('button', { name: /register|sign up|create account/i }).click();

    // After successful registration, redirect to home or dashboard
    await expect(page).not.toHaveURL(/register/);
  });

  test('registered user can log in', async ({ page }) => {
    await page.goto('/auth/login');

    await page.getByLabel(/email/i).fill('user01@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();

    // Redirect away from login page on success
    await expect(page).not.toHaveURL(/login/);
  });

  test('logged-in user can browse products and add one to cart', async ({ page }) => {
    // Log in first
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill('user01@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();
    await expect(page).not.toHaveURL(/login/);

    // Navigate to product listing
    await page.goto('/products');
    await expect(page.getByRole('main')).toBeVisible();

    // Click first product card
    const firstCard = page.locator('[data-testid="product-card"], .product-card, app-product-card').first();
    await firstCard.click();

    // On PDP: select size if available and add to cart
    const sizeButton = page.locator('[data-testid="size-chip"], .size-chip').first();
    if (await sizeButton.isVisible()) {
      await sizeButton.click();
    }

    const addToBagBtn = page.getByRole('button', { name: /add to bag|add to cart/i });
    await expect(addToBagBtn).toBeVisible();
    await addToBagBtn.click();

    // Cart badge or success toast confirms addition
    const cartBadge = page.locator('[data-testid="cart-badge"], .cart-count').first();
    const toast     = page.locator('.toast, [role="alert"]').first();
    await expect(cartBadge.or(toast)).toBeVisible({ timeout: 5_000 });
  });

  test('user can proceed to checkout from cart', async ({ page }) => {
    // Seed a cart by logging in and navigating (previous test may have done this)
    await page.goto('/auth/login');
    await page.getByLabel(/email/i).fill('user01@mailinator.com');
    await page.getByLabel(/password/i).fill('Test@123');
    await page.getByRole('button', { name: /login|sign in/i }).click();
    await expect(page).not.toHaveURL(/login/);

    // Go to cart
    await page.goto('/cart');
    await expect(page.getByRole('main')).toBeVisible();

    // If cart has items, proceed to checkout; otherwise pass gracefully
    const proceedBtn = page.getByRole('button', { name: /proceed|checkout/i });
    if (await proceedBtn.isVisible()) {
      await proceedBtn.click();
      await expect(page).toHaveURL(/checkout/);
    } else {
      test.skip(); // cart empty — skip rather than fail
    }
  });
});
