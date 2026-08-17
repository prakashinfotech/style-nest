/**
 * ENH-INFRA-012 — App Insights RUM: Core Web Vitals measurement and alerting.
 *
 * Measures LCP, CLS, INP, FID and TTFB using the Google `web-vitals` library, then:
 *   1. Sends each metric to Azure Application Insights via the JS SDK (if connection string configured)
 *   2. Sends each metric to Google Analytics 4 via window.gtag as a custom event
 *   3. Logs a performance warning when a metric exceeds the p75 budget from SOW §4 / NFR-PERF-001..003
 *
 * Alerting thresholds (from NFR-PERF-001..003):
 *   LCP  p75 < 2 500 ms   (Good)
 *   INP  p75 <   200 ms   (Good)
 *   CLS  p75 <     0.1    (Good)
 *   TTFB p75 <   800 ms   (Good)
 *
 * The measurement runs once on app init via APP_INITIALIZER — the `web-vitals` library
 * fires callbacks when the browser is idle after each interaction / page load, so there
 * is no polling overhead.
 */
import { Injectable, inject } from '@angular/core';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import type { Metric } from 'web-vitals';
import { environment } from '../../../environments/environment';

// ── NFR thresholds (Good tier) ────────────────────────────────────────────────
// FID removed — deprecated by Google in web-vitals v4+; INP is the replacement.
const BUDGET: Record<string, number> = {
  LCP:  2500,
  INP:   200,
  CLS:     0.1,
  TTFB:  800,
};

@Injectable({ providedIn: 'root' })
export class WebVitalsService {
  private appInsights: ApplicationInsights | null = null;

  /**
   * Called once at startup via APP_INITIALIZER.
   * Initialises App Insights (if configured) and subscribes to web-vitals callbacks.
   */
  async init(): Promise<void> {
    if (typeof window === 'undefined') return; // SSR guard

    this.initAppInsights();
    await this.startMeasuring();
  }

  // ── App Insights ──────────────────────────────────────────────────────────

  private initAppInsights(): void {
    const connStr = environment.analytics.appInsightsConnectionStr;
    if (!connStr || connStr.startsWith('REPLACE')) return;

    this.appInsights = new ApplicationInsights({
      config: {
        connectionString: connStr,
        enableAutoRouteTracking:    true,
        enableCorsCorrelation:      true,
        enableRequestHeaderTracking: true,
        disableFetchTracking:       false,
      },
    });
    this.appInsights.loadAppInsights();
    this.appInsights.trackPageView(); // initial page view
  }

  // ── Web Vitals measurement ─────────────────────────────────────────────────

  private async startMeasuring(): Promise<void> {
    // Dynamic import keeps `web-vitals` out of the critical bundle path
    // onFID was removed in web-vitals v4+ (FID was deprecated by Google in favour of INP).
    const { onLCP, onCLS, onINP, onTTFB } = await import('web-vitals');

    const report = (metric: Metric) => this.onMetric(metric);
    onLCP(report,  { reportAllChanges: false });
    onCLS(report,  { reportAllChanges: false });
    onINP(report,  { reportAllChanges: false });
    onTTFB(report, { reportAllChanges: false });
  }

  private onMetric(metric: Metric): void {
    const { name, value, rating } = metric;

    // ── 1. App Insights custom metric ─────────────────────────────────────
    this.appInsights?.trackMetric(
      { name: `CWV.${name}`, average: value },
      { rating, navigationType: metric.navigationType ?? 'navigate' },
    );

    // ── 2. GA4 custom event ───────────────────────────────────────────────
    const win = window as Window & { gtag?: (...args: unknown[]) => void };
    win.gtag?.('event', 'web_vitals', {
      event_category:  'Web Vitals',
      event_label:     metric.id,
      metric_name:     name,
      metric_value:    Math.round(name === 'CLS' ? value * 1000 : value), // CLS unitless → ×1000 for integer GA4 value
      metric_rating:   rating,
      non_interaction: true,
    });

    // ── 3. Budget alert (console warning) ────────────────────────────────
    const budget = BUDGET[name];
    if (budget !== undefined && value > budget) {
      console.warn(
        `[WebVitals] ⚠ ${name} ${value.toFixed(name === 'CLS' ? 3 : 0)} exceeds p75 budget (${budget}) — rating: ${rating}`,
      );
      // Also log to App Insights as a severity-2 (Warning) trace so it surfaces in Azure dashboards
      this.appInsights?.trackTrace({
        message:  `CWV budget exceeded: ${name} = ${value.toFixed(3)} (budget: ${budget})`,
        severityLevel: 2, // SeverityLevel.Warning
      });
    }
  }
}
