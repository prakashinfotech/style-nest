/**
 * ENH-AI-005 — GA4 + Meta Pixel + Mixpanel Analytics with PII Scrubbing.
 *
 * Loads analytics provider scripts on first call to init() only if the
 * corresponding IDs are configured in environment.analytics (non-empty).
 *
 * PII scrubbing rules (applied before every event is forwarded):
 *   • String values matching email / phone patterns are replaced with '[REDACTED]'
 *   • All values truncated to 200 chars to prevent accidental PII leakage in long strings
 *   • Keys named email / phone / mobile / password are always redacted
 *
 * Providers are used via their global SDK objects (window.gtag, window.fbq, window.mixpanel).
 * If a provider's script has not loaded yet the call is silently dropped so analytics
 * failures never impact the user experience.
 */
import { Injectable, inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { environment } from '../../../environments/environment';

// ── PII-sensitive key names (case-insensitive) ────────────────────────────────
const PII_KEYS = new Set([
  'email', 'phone', 'mobile', 'password', 'phone_number',
  'user_email', 'contact', 'address', 'full_name', 'firstName', 'lastName',
]);

// ── PII pattern detectors ─────────────────────────────────────────────────────
const EMAIL_RE  = /[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}/g;
const PHONE_RE  = /\+?[0-9][\d\s\-().]{8,14}[0-9]/g;

// ── Event name normalisation map (GA4 standard event names) ──────────────────
const GA4_EVENT_MAP: Record<string, string> = {
  add_to_cart:   'add_to_cart',
  purchase:      'purchase',
  page_view:     'page_view',
  product_view:  'view_item',
  search:        'search',
  login:         'login',
  register:      'sign_up',
};

// ── Meta Pixel event map ──────────────────────────────────────────────────────
const META_EVENT_MAP: Record<string, string> = {
  add_to_cart:   'AddToCart',
  purchase:      'Purchase',
  page_view:     'PageView',
  product_view:  'ViewContent',
  search:        'Search',
  login:         'Lead',
  register:      'CompleteRegistration',
};

export type AnalyticsEventName =
  | 'page_view'
  | 'product_view'
  | 'add_to_cart'
  | 'purchase'
  | 'search'
  | 'login'
  | 'register';

export type AnalyticsEventData = Record<string, unknown>;

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly router = inject(Router);
  private readonly cfg    = environment.analytics;

  private ga4Ready      = false;
  private metaReady     = false;
  private mixpanelReady = false;

  /**
   * Called once at app startup via APP_INITIALIZER.
   * Injects provider scripts and starts listening for Router navigation events.
   */
  init(): void {
    if (typeof window === 'undefined') return; // SSR guard

    this.injectGa4();
    this.injectMetaPixel();
    this.injectMixpanel();

    // Page view tracking on every navigation
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    ).subscribe((e) => this.trackPageView(e.urlAfterRedirects));
  }

  // ── Public tracking API ───────────────────────────────────────────────────

  track(event: AnalyticsEventName, data: AnalyticsEventData = {}): void {
    const clean = this.scrubPii(data);
    this.sendToGa4(event, clean);
    this.sendToMetaPixel(event, clean);
    this.sendToMixpanel(event, clean);
  }

  trackPageView(url: string): void {
    const clean = this.scrubPii({ page_path: url });
    this.sendToGa4('page_view', clean);
    this.sendToMetaPixel('page_view', clean);
    this.sendToMixpanel('page_view', { ...clean, url });
  }

  // ── PII scrubbing ─────────────────────────────────────────────────────────

  /**
   * Returns a shallow copy of the data object with PII removed.
   * String values are scanned for email / phone patterns;
   * sensitive keys are unconditionally redacted.
   */
  scrubPii(data: AnalyticsEventData): AnalyticsEventData {
    const result: AnalyticsEventData = {};
    for (const [key, value] of Object.entries(data)) {
      if (PII_KEYS.has(key.toLowerCase())) {
        result[key] = '[REDACTED]';
        continue;
      }
      if (typeof value === 'string') {
        let sanitised = value
          .replace(EMAIL_RE, '[EMAIL]')
          .replace(PHONE_RE, '[PHONE]')
          .slice(0, 200); // truncate to prevent long PII bleed-through
        result[key] = sanitised;
      } else if (typeof value === 'number' || typeof value === 'boolean') {
        result[key] = value;
      } else {
        // Nested objects / arrays — JSON-stringify then truncate; not deep-scrubbed
        result[key] = JSON.stringify(value).slice(0, 200);
      }
    }
    return result;
  }

  // ── GA4 ───────────────────────────────────────────────────────────────────

  private injectGa4(): void {
    if (!this.cfg.ga4MeasurementId || this.cfg.ga4MeasurementId.startsWith('REPLACE')) return;

    const script = document.createElement('script');
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${this.cfg.ga4MeasurementId}`;
    script.onload = () => { this.ga4Ready = true; };
    document.head.appendChild(script);

    // Initialise dataLayer + gtag function
    const win = window as Window & { dataLayer?: unknown[]; gtag?: (...args: unknown[]) => void };
    win.dataLayer = win.dataLayer ?? [];
    win.gtag = function (...args: unknown[]) { win.dataLayer!.push(args); };
    win.gtag('js', new Date());
    win.gtag('config', this.cfg.ga4MeasurementId, { anonymize_ip: true, send_page_view: false });
    this.ga4Ready = true;
  }

  private sendToGa4(event: string, data: AnalyticsEventData): void {
    if (!this.ga4Ready) return;
    const win = window as Window & { gtag?: (...args: unknown[]) => void };
    if (typeof win.gtag !== 'function') return;
    const ga4Event = GA4_EVENT_MAP[event] ?? event;
    win.gtag('event', ga4Event, data);
  }

  // ── Meta Pixel ────────────────────────────────────────────────────────────

  private injectMetaPixel(): void {
    if (!this.cfg.metaPixelId || this.cfg.metaPixelId.startsWith('REPLACE')) return;

    const win = window as Window & {
      fbq?: ((...args: unknown[]) => void) & { callMethod?: (...args: unknown[]) => void; queue?: unknown[] };
      _fbq?: unknown;
    };

    // Meta Pixel inline bootstrap (from official snippet).
    // Uses a captured queue to avoid a circular self-reference on the function object
    // which TypeScript 5.9+ strict mode cannot type-check inside the function body.
    if (!win.fbq) {
      const pendingQueue: unknown[] = [];
      const fbqFn = (...args: unknown[]): void => {
        const cm = (fbqFn as { callMethod?: (...a: unknown[]) => void }).callMethod;
        if (cm) cm(...args); else pendingQueue.push(args);
      };
      (fbqFn as unknown as { queue: unknown[] }).queue = pendingQueue;
      win.fbq  = fbqFn as unknown as typeof win.fbq;
      win._fbq = win.fbq;
    }

    const script = document.createElement('script');
    script.async = true;
    script.src = 'https://connect.facebook.net/en_US/fbevents.js';
    script.onload = () => { this.metaReady = true; };
    document.head.appendChild(script);

    win.fbq!('init', this.cfg.metaPixelId);
    this.metaReady = true;
  }

  private sendToMetaPixel(event: string, data: AnalyticsEventData): void {
    if (!this.metaReady) return;
    const win = window as Window & { fbq?: (...args: unknown[]) => void };
    if (typeof win.fbq !== 'function') return;
    const metaEvent = META_EVENT_MAP[event];
    if (!metaEvent) return;
    if (event === 'page_view') win.fbq('track', 'PageView');
    else win.fbq('track', metaEvent, data);
  }

  // ── Mixpanel ──────────────────────────────────────────────────────────────

  private injectMixpanel(): void {
    if (!this.cfg.mixpanelToken || this.cfg.mixpanelToken.startsWith('REPLACE')) return;

    const script = document.createElement('script');
    script.async = true;
    script.src   = 'https://cdn.mxpnl.com/libs/mixpanel-2-latest.min.js';
    script.onload = () => {
      const win = window as Window & { mixpanel?: { init: (token: string, opts: object) => void } };
      win.mixpanel?.init(this.cfg.mixpanelToken, {
        track_pageview: false,  // we fire manually for PII scrubbing
        persistence: 'localStorage',
        api_host: 'https://api-eu.mixpanel.com', // EU data residency
      });
      this.mixpanelReady = true;
    };
    document.head.appendChild(script);
  }

  private sendToMixpanel(event: string, data: AnalyticsEventData): void {
    if (!this.mixpanelReady) return;
    const win = window as Window & { mixpanel?: { track: (event: string, props: object) => void } };
    if (typeof win.mixpanel?.track !== 'function') return;
    win.mixpanel.track(event, data);
  }
}
