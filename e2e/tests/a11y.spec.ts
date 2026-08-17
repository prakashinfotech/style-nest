/**
 * ENH-INFRA-010 — axe-core Accessibility CI Gate
 * NFR-A11Y-001: WCAG 2.1 AA, 0 critical violations on key storefront routes.
 *
 * Run: npx playwright test tests/a11y.spec.ts
 * CI:  .github/workflows/a11y.yml
 */

import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const ROUTES: { name: string; path: string }[] = [
  { name: "Homepage",        path: "/" },
  { name: "Product List",    path: "/products" },
  { name: "Login Page",      path: "/login" },
  { name: "Register Page",   path: "/register" },
  { name: "Cart Page",       path: "/cart" },
];

for (const route of ROUTES) {
  test(`WCAG 2.1 AA — ${route.name} (${route.path})`, async ({ page }) => {
    await page.goto(route.path);

    // Wait for Angular router to stabilise
    await page.waitForLoadState("networkidle");

    const results = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa", "wcag21aa"])
      .exclude("[aria-hidden='true']") // skip explicitly hidden elements
      .analyze();

    // Collect only critical and serious violations
    const critical = results.violations.filter(
      (v) => v.impact === "critical" || v.impact === "serious"
    );

    // Print violations for CI log visibility
    if (critical.length > 0) {
      console.error(
        `axe violations on ${route.path}:\n` +
          critical
            .map(
              (v) =>
                `  [${v.impact}] ${v.id}: ${v.description} — ` +
                v.nodes.map((n) => n.html).join(", ")
            )
            .join("\n")
      );
    }

    expect(
      critical,
      `Found ${critical.length} critical/serious WCAG 2.1 AA violation(s) on ${route.path}`
    ).toHaveLength(0);
  });
}
