import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  Input,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { ViewChild, TemplateRef, ViewContainerRef } from '@angular/core';
import { SkeletonLoaderComponent } from '../shared/components/skeleton-loader.component';
import { Product360ViewComponent } from './product-360-view.component';

@Component({
  selector: 'app-product-images',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, SkeletonLoaderComponent, Product360ViewComponent],
  styles: [`
    .zoom-container { overflow: hidden; }
    .zoom-container img { transition: transform 0.3s ease; }
    .zoom-container:hover img { transform: scale(1.5); }
  `],
  template: `
    <!-- ENH-PDP-007: View-mode tabs (Photos / 360°) shown only when has360View=true -->
    @if (has360View && images.length > 0) {
      <div class="flex gap-2 mb-3" role="tablist" [attr.aria-label]="'Product view modes for ' + productName">
        <button
          role="tab"
          [attr.aria-selected]="viewMode() === 'photos'"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold
                 border transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
          [class.bg-navy]="viewMode() === 'photos'"
          [class.text-white]="viewMode() === 'photos'"
          [class.border-navy]="viewMode() === 'photos'"
          [class.bg-white]="viewMode() !== 'photos'"
          [class.text-dark]="viewMode() !== 'photos'"
          [class.border-border]="viewMode() !== 'photos'"
          (click)="viewMode.set('photos')"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
          </svg>
          Photos
        </button>
        <button
          role="tab"
          [attr.aria-selected]="viewMode() === '360'"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold
                 border transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
          [class.bg-navy]="viewMode() === '360'"
          [class.text-white]="viewMode() === '360'"
          [class.border-navy]="viewMode() === '360'"
          [class.bg-white]="viewMode() !== '360'"
          [class.text-dark]="viewMode() !== '360'"
          [class.border-border]="viewMode() !== '360'"
          (click)="viewMode.set('360')"
        >
          <!-- Rotate icon -->
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          360° View
        </button>
      </div>
    }

    <!-- 360° view panel (ENH-PDP-007) -->
    @if (viewMode() === '360' && has360View) {
      <app-product-360-view
        [frames]="images"
        [productName]="productName"
      />
    }

    <!-- Standard photo gallery — hidden when 360° tab is active -->
    @if (viewMode() === 'photos') {
    <div class="flex gap-3">

      <!-- Thumbnails — desktop only -->
      <div class="hidden md:flex flex-col gap-2 w-16 flex-shrink-0">
        @for (img of images; track img; let i = $index) {
          <button
            class="w-16 h-20 border-2 rounded overflow-hidden flex-shrink-0 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red"
            [class.border-navy]="activeIndex() === i"
            [class.border-transparent]="activeIndex() !== i"
            [attr.aria-label]="'View image ' + (i + 1) + ' of ' + images.length"
            (click)="activeIndex.set(i)"
          >
            <img
              [src]="img"
              [alt]="productName + ' thumbnail ' + (i + 1)"
              class="w-full h-full object-cover"
              loading="lazy"
            />
          </button>
        }
        @if (images.length === 0) {
          <div class="w-16 h-20 bg-gray-100 rounded"></div>
        }
      </div>

      <!-- Main image -->
      <div
        class="flex-1 aspect-[3/4] rounded-lg overflow-hidden bg-gray-50 relative zoom-container"
        tabindex="0"
        role="img"
        [attr.aria-label]="productName + ' — image ' + (activeIndex() + 1) + ' of ' + images.length"
        (keydown.ArrowLeft)="prev()"
        (keydown.ArrowRight)="next()"
        (touchstart)="onTouchStart($event)"
        (touchend)="onTouchEnd($event)"
      >
        @if (images.length > 0) {
          <!-- Skeleton shown until image loads (LCP placeholder) -->
          @if (!imageLoaded()) {
            <div class="absolute inset-0 z-10">
              <app-skeleton-loader height="100%" cssClass="rounded-lg" />
            </div>
          }
          <img
            [src]="images[activeIndex()]"
            [alt]="productName + ' — image ' + (activeIndex() + 1)"
            class="w-full h-full object-contain cursor-zoom-in"
            [loading]="activeIndex() === 0 ? 'eager' : 'lazy'"
            [class.opacity-0]="!imageLoaded()"
            [class.opacity-100]="imageLoaded()"
            style="transition: opacity 0.2s ease"
            (load)="imageLoaded.set(true)"
            (click)="openLightbox()"
          />
        } @else {
          <div class="w-full h-full flex items-center justify-center text-8xl" aria-hidden="true">🛍️</div>
        }

        <!-- Mobile counter pill — "2 / 5" -->
        @if (images.length > 1) {
          <div
            class="md:hidden absolute bottom-3 right-3 bg-black/60 text-white text-xs font-medium
                   px-2 py-0.5 rounded-full pointer-events-none"
            aria-hidden="true"
          >{{ activeIndex() + 1 }} / {{ images.length }}</div>
        }

        <!-- Mobile dots (small screens) -->
        @if (images.length > 1) {
          <div class="md:hidden absolute bottom-3 left-1/2 -translate-x-1/2 flex gap-1.5" aria-hidden="true">
            @for (img of images; track img; let i = $index) {
              <button
                class="w-1.5 h-1.5 rounded-full transition"
                [class.bg-navy]="activeIndex() === i"
                [class.bg-gray-300]="activeIndex() !== i"
                [attr.aria-label]="'Go to image ' + (i + 1)"
                (click)="activeIndex.set(i); imageLoaded.set(false)"
              ></button>
            }
          </div>
        }

        <!-- Desktop prev/next arrows -->
        @if (images.length > 1) {
          <button
            class="hidden md:flex absolute left-2 top-1/2 -translate-y-1/2 w-8 h-8 bg-white/80
                   rounded-full items-center justify-center shadow hover:bg-white transition
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red"
            tabindex="0"
            aria-label="Previous image"
            (click)="prev()"
          >
            <svg class="w-4 h-4 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
          </button>
          <button
            class="hidden md:flex absolute right-2 top-1/2 -translate-y-1/2 w-8 h-8 bg-white/80
                   rounded-full items-center justify-center shadow hover:bg-white transition
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red"
            tabindex="0"
            aria-label="Next image"
            (click)="next()"
          >
            <svg class="w-4 h-4 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </button>
        }
      </div>
    </div>
    } <!-- end @if (viewMode() === 'photos') -->

    <!-- Lightbox template — rendered into CDK Overlay -->
    <ng-template #lightboxTpl>
      <div
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/90"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="productName + ' fullscreen image'"
        (click)="closeLightbox()"
        (keydown.Escape)="closeLightbox()"
      >
        <!-- Stop propagation so clicking the image itself doesn't close -->
        <div class="relative max-w-3xl max-h-[90vh] p-4" (click)="$event.stopPropagation()">
          <img
            [src]="images[activeIndex()]"
            [alt]="productName + ' — fullscreen image ' + (activeIndex() + 1)"
            class="max-w-full max-h-[80vh] object-contain rounded-lg"
            loading="eager"
          />
          <!-- Close button -->
          <button
            class="absolute top-2 right-2 w-9 h-9 bg-white/20 hover:bg-white/40 rounded-full
                   flex items-center justify-center text-white transition
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
            aria-label="Close fullscreen image"
            (click)="closeLightbox()"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
          <!-- Lightbox counter -->
          <div class="absolute bottom-6 left-1/2 -translate-x-1/2 text-white/80 text-sm">
            {{ activeIndex() + 1 }} / {{ images.length }}
          </div>
        </div>
      </div>
    </ng-template>
  `,
})
export class ProductImagesComponent implements OnDestroy {
  @Input({ required: true }) images: string[] = [];
  @Input() productName = '';
  /** ENH-PDP-007 — When true, shows the "360° View" tab alongside "Photos". */
  @Input() has360View = false;

  @ViewChild('lightboxTpl') lightboxTpl!: TemplateRef<unknown>;

  /** ENH-PDP-007 — Active tab: 'photos' (default) or '360'. */
  readonly viewMode = signal<'photos' | '360'>('photos');

  private readonly overlay        = inject(Overlay);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private overlayRef: OverlayRef | null = null;

  readonly activeIndex = signal(0);
  readonly imageLoaded = signal(false);

  // ── Navigation ────────────────────────────────────────────────────────────

  prev(): void {
    this.imageLoaded.set(false);
    this.activeIndex.update((i) => (i - 1 + this.images.length) % this.images.length);
  }

  next(): void {
    this.imageLoaded.set(false);
    this.activeIndex.update((i) => (i + 1) % this.images.length);
  }

  // ── Touch swipe ───────────────────────────────────────────────────────────

  private touchStartX = 0;

  onTouchStart(e: TouchEvent): void {
    this.touchStartX = e.changedTouches[0].clientX;
  }

  onTouchEnd(e: TouchEvent): void {
    const delta = e.changedTouches[0].clientX - this.touchStartX;
    if (Math.abs(delta) < 40) return; // ignore small taps
    delta < 0 ? this.next() : this.prev();
  }

  // ── Lightbox ──────────────────────────────────────────────────────────────

  openLightbox(): void {
    if (this.overlayRef) return;
    this.overlayRef = this.overlay.create({
      hasBackdrop: false,
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
    });
    const portal = new TemplatePortal(this.lightboxTpl, this.viewContainerRef);
    this.overlayRef.attach(portal);
  }

  closeLightbox(): void {
    this.overlayRef?.dispose();
    this.overlayRef = null;
  }

  @HostListener('document:keydown.Escape')
  onEscape(): void { this.closeLightbox(); }

  ngOnDestroy(): void { this.closeLightbox(); }
}
