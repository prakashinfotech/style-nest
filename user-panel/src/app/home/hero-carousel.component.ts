import {
  ChangeDetectionStrategy, Component, HostListener,
  OnDestroy, OnInit, inject, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExperimentService } from '../core/services/experiment.service';

interface HeroSlide {
  imageUrl: string;
  eyebrow:  string;
  title:    string;
  subtitle: string;
  ctaLabel: string;
  ctaLink:  string;
  bgColor:  string;
}

// ── ENH-CAT-003 — A/B Variant slide sets ─────────────────────────────────────
// Variant A (control):  "New Arrivals Are Here" messaging
// Variant B (treatment): "Fashion Forward" urgency-led headline
// Slides 2 & 3 are identical across variants — only slide 1 is tested.

const SLIDES_VARIANT_A: HeroSlide[] = [
  {
    imageUrl: 'https://images.unsplash.com/photo-1483985988355-763728e1935b?q=80&w=2070&auto=format&fit=crop',
    eyebrow:  'New Season · Spring / Summer 2026',
    title:    'New Arrivals Are Here',
    subtitle: 'Up to 50% off on premium fashion — Limited time offer',
    ctaLabel: 'Shop Women',
    ctaLink:  '/products?category=women',
    bgColor:  'linear-gradient(135deg, #1C2B4A 0%, #2C3E70 100%)',
  },
  {
    imageUrl: 'https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=2070&auto=format&fit=crop',
    eyebrow:  'Curated · Authentic · Exclusive',
    title:    'Luxury Redefined',
    subtitle: 'Discover our handpicked collection of premium brands',
    ctaLabel: 'Shop Luxury',
    ctaLink:  '/products?category=luxury',
    bgColor:  'linear-gradient(135deg, #1A1A1A 0%, #424242 100%)',
  },
  {
    imageUrl: 'https://images.unsplash.com/photo-1549439602-43ebca2327af?q=80&w=2070&auto=format&fit=crop',
    eyebrow:  'Sale Picks · Up to 70% Off',
    title:    'The Big Fashion Sale',
    subtitle: 'Biggest discounts of the season across top brands',
    ctaLabel: 'Shop Sale',
    ctaLink:  '/products?category=sale',
    bgColor:  'linear-gradient(90deg, #E31837 0%, #FF6B35 100%)',
  },
];

const SLIDES_VARIANT_B: HeroSlide[] = [
  {
    // Treatment: urgency-led headline, discount-first framing, broader CTA
    imageUrl: 'https://images.unsplash.com/photo-1483985988355-763728e1935b?q=80&w=2070&auto=format&fit=crop',
    eyebrow:  'Limited Time · Prices Drop At Midnight',
    title:    'Fashion Forward — Up to 60% Off',
    subtitle: 'Fresh drops, top brands, and premium markdowns — every day',
    ctaLabel: 'Shop Now',
    ctaLink:  '/products',
    bgColor:  'linear-gradient(135deg, #1C2B4A 0%, #2C3E70 100%)',
  },
  ...SLIDES_VARIANT_A.slice(1), // slides 2 & 3 identical across variants
];

@Component({
  selector: 'app-hero-carousel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!--
      DESIGN.md §4.4 Hero Carousel
      ─ CSS-driven h-[260px] md:h-[480px] (no JS window.innerWidth)
      ─ Progress bar restarted via @for keyed DOM recreation
      ─ Pause captures elapsed % so bar freezes at correct position
      ─ Prev/Next/GoTo restart the full setInterval so timing is always correct
      ─ focusin/focusout with 50ms debounce prevents flicker on inter-button tab
      ─ Touch swipe: 50px threshold
    -->
    <section
      class="relative w-full overflow-hidden bg-navy h-[260px] md:h-[480px] select-none focus:outline-none"
      aria-label="Featured promotions"
      aria-roledescription="carousel"
      tabindex="0"
      (mouseenter)="pause()"
      (mouseleave)="resume()"
      (focusin)="onFocusIn()"
      (focusout)="onFocusOut()"
      (touchstart)="onTouchStart($event)"
      (touchend)="onTouchEnd($event)"
    >

      <!-- ── Slides ───────────────────────────────────────────── -->
      @for (slide of slides; track slide.title; let i = $index) {
        <div
          class="absolute inset-0 flex items-end transition-opacity duration-500"
          [class.opacity-100]="currentIndex() === i"
          [class.opacity-0]="currentIndex() !== i"
          [class.pointer-events-none]="currentIndex() !== i"
          [attr.aria-hidden]="currentIndex() !== i ? 'true' : null"
          role="group"
          [attr.aria-roledescription]="'Slide ' + (i + 1) + ' of ' + slides.length"
          [attr.aria-label]="slide.title"
        >
          <!-- Transparent full-slide link — z-[1] sits above bg, below text (z-10) -->
          <a
            [routerLink]="routerPath(slide.ctaLink)"
            [queryParams]="queryParams(slide.ctaLink)"
            class="absolute inset-0 z-[1] cursor-pointer"
            tabindex="-1"
            aria-hidden="true"
          ></a>

          <!-- Background -->
          @if (slide.imageUrl) {
            <img
              [src]="slide.imageUrl"
              [alt]=""
              draggable="false"
              class="absolute inset-0 w-full h-full object-cover pointer-events-none"
              [attr.loading]="i === 0 ? 'eager' : 'lazy'"
              [attr.fetchpriority]="i === 0 ? 'high' : 'auto'"
              aria-hidden="true"
            />
            <div
              class="absolute inset-0 bg-gradient-to-r from-black/75 via-black/40 to-transparent pointer-events-none"
              aria-hidden="true"
            ></div>
          } @else {
            <div
              class="absolute inset-0 pointer-events-none"
              [style.background]="slide.bgColor"
              aria-hidden="true"
            ></div>
          }

          <!-- Text block — z-10 keeps it above the clickable overlay -->
          <div class="max-w-layout mx-auto px-4 md:px-16 w-full pb-10 md:pb-16 relative z-10">
            <div class="max-w-sm md:max-w-xl">

              <p class="text-[11px] md:text-[12px] tracking-widest uppercase text-white/80 font-medium mb-2 md:mb-3">
                {{ slide.eyebrow }}
              </p>

              <h2 class="font-display text-[22px] md:text-[42px] font-bold text-white mb-2 md:mb-4 leading-tight">
                {{ slide.title }}
              </h2>

              <p class="hidden sm:block text-sm md:text-base text-white/70 font-light mb-5 md:mb-7">
                {{ slide.subtitle }}
              </p>

              <a
                [routerLink]="routerPath(slide.ctaLink)"
                [queryParams]="queryParams(slide.ctaLink)"
                class="inline-block bg-red text-white font-semibold px-6 py-2.5 md:px-8 md:py-3 rounded-md
                       text-sm md:text-base hover:bg-red/90 active:scale-[0.97]
                       transition-all duration-200
                       focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-transparent"
                [attr.aria-label]="slide.ctaLabel + ' — ' + slide.title"
              >
                {{ slide.ctaLabel }}
              </a>

            </div>
          </div>
        </div>
      }

      <!-- ── Prev / Next arrows ─────────────────────────────── -->
      <button
        type="button"
        class="absolute left-3 md:left-5 top-1/2 -translate-y-1/2
               bg-white/20 hover:bg-white/40 active:bg-white/50 text-white rounded-full p-2
               transition-colors duration-200 min-h-[44px] min-w-[44px] flex items-center justify-center
               z-20 focus-visible:ring-2 focus-visible:ring-white"
        aria-label="Previous slide"
        (click)="prev()"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
      </button>

      <button
        type="button"
        class="absolute right-3 md:right-5 top-1/2 -translate-y-1/2
               bg-white/20 hover:bg-white/40 active:bg-white/50 text-white rounded-full p-2
               transition-colors duration-200 min-h-[44px] min-w-[44px] flex items-center justify-center
               z-20 focus-visible:ring-2 focus-visible:ring-white"
        aria-label="Next slide"
        (click)="next()"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
        </svg>
      </button>

      <!-- ── Dot indicators ─────────────────────────────────── -->
      <div
        class="absolute bottom-5 left-1/2 -translate-x-1/2 flex gap-2 items-center z-20"
        role="tablist"
        aria-label="Slide indicators"
      >
        @for (slide of slides; track slide.title; let i = $index) {
          <button
            type="button"
            class="h-2 rounded-full transition-all duration-300"
            [class.bg-white]="currentIndex() === i"
            [class.w-6]="currentIndex() === i"
            [class.bg-white/40]="currentIndex() !== i"
            [class.w-2]="currentIndex() !== i"
            role="tab"
            [attr.aria-selected]="currentIndex() === i ? 'true' : 'false'"
            [attr.aria-label]="'Go to slide ' + (i + 1) + ': ' + slide.title"
            (click)="goTo(i)"
          ></button>
        }
      </div>

      <!-- ── Progress bar ───────────────────────────────────── -->
      <!--
        The @for trick forces DOM recreation each time progressKey() changes.
        A brand-new element starts the CSS animation from 0% automatically.
        Without this, the browser keeps the old element and the animation
        never restarts regardless of re-applying [style.animation].
      -->
      <div class="absolute bottom-0 inset-x-0 h-[3px] bg-white/20 z-20 overflow-hidden" aria-hidden="true">
        @if (!paused()) {
          @for (k of [progressKey()]; track k) {
            <div
              class="h-full bg-red"
              style="animation-name: progress-fill; animation-timing-function: linear; animation-fill-mode: forwards;"
              [style.animation-duration]="slideDuration + 'ms'"
            ></div>
          }
        } @else {
          <!-- Freeze bar at position where pause occurred -->
          <div class="h-full bg-red transition-none" [style.width]="pausedAt() + '%'"></div>
        }
      </div>

    </section>
  `,
})
export class HeroCarouselComponent implements OnInit, OnDestroy {
  readonly currentIndex = signal(0);
  readonly paused       = signal(false);
  readonly progressKey  = signal(0);
  /** Percentage 0–100 at which the bar was frozen when pause() was called */
  readonly pausedAt     = signal(0);

  readonly slideDuration = 5000;

  private timer:            ReturnType<typeof setInterval>  | null = null;
  private focusOutDebounce: ReturnType<typeof setTimeout>   | null = null;
  private slideStartTime = 0;
  private touchStartX    = 0;

  // ENH-CAT-003 — resolve experiment variant synchronously at field-init time
  private readonly _abVariant = inject(ExperimentService).getVariant('hero-cta', ['A', 'B']);
  readonly slides: HeroSlide[] = this._abVariant === 'B' ? SLIDES_VARIANT_B : SLIDES_VARIANT_A;

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowLeft')  { event.preventDefault(); this.prev(); }
    if (event.key === 'ArrowRight') { event.preventDefault(); this.next(); }
  }

  ngOnInit(): void {
    this.startAutoAdvance();
  }

  ngOnDestroy(): void {
    this.clearTimer();
    if (this.focusOutDebounce) clearTimeout(this.focusOutDebounce);
  }

  // ── Pause / Resume ──────────────────────────────────────────

  pause(): void {
    if (this.paused()) return;
    const elapsed = Date.now() - this.slideStartTime;
    this.pausedAt.set(Math.min(100, (elapsed / this.slideDuration) * 100));
    this.paused.set(true);
    this.clearTimer();
  }

  resume(): void {
    if (!this.paused()) return;
    this.paused.set(false);
    this.startAutoAdvance();
  }

  // ── focusin/focusout with 50ms debounce ─────────────────────
  // Prevents rapid pause→resume→pause when focus moves between
  // sibling buttons inside the carousel (each tab triggers focusout
  // then focusin in quick succession).

  onFocusIn(): void {
    if (this.focusOutDebounce) {
      clearTimeout(this.focusOutDebounce);
      this.focusOutDebounce = null;
    }
    this.pause();
  }

  onFocusOut(): void {
    this.focusOutDebounce = setTimeout(() => {
      this.focusOutDebounce = null;
      this.resume();
    }, 50);
  }

  // ── Navigation ──────────────────────────────────────────────

  next(): void {
    this.currentIndex.update((i) => (i + 1) % this.slides.length);
    this.onManualNav();
  }

  prev(): void {
    this.currentIndex.update((i) => (i - 1 + this.slides.length) % this.slides.length);
    this.onManualNav();
  }

  goTo(index: number): void {
    if (index === this.currentIndex()) return;
    this.currentIndex.set(index);
    this.onManualNav();
  }

  // ── Touch swipe ─────────────────────────────────────────────

  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0].clientX;
  }

  onTouchEnd(event: TouchEvent): void {
    const dx = event.changedTouches[0].clientX - this.touchStartX;
    if (dx >  50) this.prev();
    if (dx < -50) this.next();
  }

  // ── URL helpers ─────────────────────────────────────────────

  routerPath(url: string): string {
    return url.split('?')[0];
  }

  queryParams(url: string): Record<string, string> {
    const i = url.indexOf('?');
    if (i === -1) return {};
    return Object.fromEntries(new URLSearchParams(url.slice(i + 1)).entries());
  }

  // ── Private ─────────────────────────────────────────────────

  /**
   * Called by prev/next/goTo.
   * Resets the progress bar and — if not paused — fully restarts the
   * setInterval so the NEXT auto-advance fires exactly slideDuration ms
   * from now (not from whenever the previous interval was started).
   */
  private onManualNav(): void {
    this.pausedAt.set(0);
    if (this.paused()) {
      this.slideStartTime = Date.now();
      this.progressKey.update((k) => k + 1);
    } else {
      this.startAutoAdvance();
    }
  }

  private startAutoAdvance(): void {
    this.clearTimer();
    this.slideStartTime = Date.now();
    this.progressKey.update((k) => k + 1);
    this.timer = setInterval(() => {
      this.currentIndex.update((i) => (i + 1) % this.slides.length);
      this.slideStartTime = Date.now();
      this.progressKey.update((k) => k + 1);
    }, this.slideDuration);
  }

  private clearTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
