import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Inject,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { Store } from '@ngrx/store';
import { Subscription, switchMap } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { CatalogActions } from '../../store/catalog/catalog.actions';
import { selectSelectedProduct, selectPdpLoading, selectRecentlyViewed } from '../../store/catalog/catalog.selectors';
import { Product, ProductVariant } from '../../core/models/product.model';
import { getColourHex } from '../../core/utils/colour-map';
import { ColourOption } from '../../catalog/colour-selector.component';
import { ProductImagesComponent } from '../../catalog/product-images.component';
import { ProductInfoComponent } from '../../catalog/product-info.component';
import { SizeSelectorComponent } from '../../catalog/size-selector.component';
import { ColourSelectorComponent } from '../../catalog/colour-selector.component';
import { AddToCartPanelComponent } from '../../catalog/add-to-cart-panel.component';
import { ProductDescriptionComponent } from '../../catalog/product-description.component';
import { ProductReviewsComponent } from '../../catalog/product-reviews.component';
import { RelatedProductsComponent } from '../../catalog/related-products.component';
import { StickyProductBarComponent } from '../../catalog/sticky-product-bar.component';
import { SizeGuideModalComponent } from '../../catalog/size-guide-modal.component';
import { SkeletonLoaderComponent } from '../../shared/components/skeleton-loader.component';
import { BreadcrumbComponent } from '../../shared/components/breadcrumb.component';
import { SectionHeaderComponent } from '../../shared/components/section-header.component';
import { ProductCardComponent } from '../../catalog/product-card.component';
import { selectWishlistIds } from '../../store/wishlist/wishlist.selectors';

@Component({
  selector: 'app-pdp',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ProductImagesComponent,
    ProductInfoComponent,
    SizeSelectorComponent, ColourSelectorComponent,
    AddToCartPanelComponent, ProductDescriptionComponent,
    ProductReviewsComponent, RelatedProductsComponent,
    StickyProductBarComponent, SizeGuideModalComponent,
    SkeletonLoaderComponent, BreadcrumbComponent,
    SectionHeaderComponent, ProductCardComponent,
  ],
  template: `
    <!-- DESIGN.md §5.3 PDP -->

    <!-- Sticky product bar (PDP-9) — appears when ATC panel scrolls out of view -->
    @if (product()) {
      <app-sticky-product-bar
        [product]="product()!"
        [visible]="showStickyBar()"
        [selectedSize]="selectedSize()"
        [selectedColour]="selectedColour()"
        [requiresSize]="hasNonOneSize()"
        [requiresColour]="availableColours().length > 0"
        [quantity]="quantity()"
      />
    }

    <div class="max-w-layout mx-auto px-4 py-6 min-h-screen pb-24 md:pb-6">

      @if (isLoading()) {
        <!-- Loading skeleton -->
        <div class="flex flex-col md:flex-row gap-8">
          <div class="md:w-3/5">
            <app-skeleton-loader height="500px" cssClass="rounded-lg" />
          </div>
          <div class="md:w-2/5 space-y-4">
            <app-skeleton-loader height="14px" width="55%" />
            <app-skeleton-loader height="20px" width="40%" />
            <app-skeleton-loader height="32px" />
            <app-skeleton-loader height="24px" width="30%" />
            <app-skeleton-loader height="16px" width="60%" />
            <app-skeleton-loader height="48px" />
          </div>
        </div>
      } @else if (product()) {

        <!-- Breadcrumb — DESIGN.md §4.17 -->
        <app-breadcrumb
          [crumbs]="[
            { label: 'Home',     link: '/' },
            { label: 'Products', link: '/products' },
            { label: product()!.categoryName, link: '/products' },
            { label: product()!.name }
          ]"
        />

        <div class="flex flex-col md:flex-row gap-8">

          <!-- Images — sticky on desktop -->
          <div class="md:w-3/5 md:sticky md:top-20 md:self-start">
            <!-- ENH-PDP-007: passes has360View so the tab toggle is shown -->
            <app-product-images
              [images]="product()!.imageUrls"
              [productName]="product()!.name"
              [has360View]="product()!.has360View ?? false"
            />
          </div>

          <!-- Details panel -->
          <div class="md:w-2/5 space-y-5">
            <!-- PDP-8: h1 target for focus on route activate -->
            <div #productHeading tabindex="-1" class="outline-none">
              <app-product-info
                [product]="product()!"
                [variantPrice]="selectedVariant()?.priceOverride ?? null"
              />
            </div>

            <!-- Size selector — variant-aware -->
            @if (product()!.variants.length > 0) {
              <app-size-selector
                [variants]="product()!.variants"
                [selectedSize]="selectedSize()"
                (sizeChange)="onSizeChange($event)"
                (sizeGuideClick)="showSizeGuide.set(true)"
              />
            }

            <!-- Colour selector — stock-aware for selected size -->
            @if (availableColours().length > 0) {
              <app-colour-selector
                [colours]="availableColours()"
                [selectedColour]="selectedColour()"
                [variants]="product()!.variants"
                [selectedSize]="selectedSize()"
                (colourChange)="selectedColour.set($event)"
              />
            }

            <!-- ATC panel — hidden on mobile (replaced by sticky bar below) -->
            <!-- #atcPanel used by IntersectionObserver for sticky bar trigger -->
            <div class="hidden md:block" #atcPanel>
              <app-add-to-cart-panel
                [product]="product()!"
                [selectedSize]="selectedSize()"
                [selectedColour]="selectedColour()"
                [requiresSize]="hasNonOneSize()"
                [requiresColour]="availableColours().length > 0"
                [quantity]="quantity()"
                [variantStock]="selectedVariant()?.stockQuantity ?? null"
                (quantityChange)="quantity.set($event)"
              />
            </div>

            <!-- Share button (PDP-9) -->
            <div class="flex items-center gap-2 pt-1">
              <button
                class="flex items-center gap-1.5 text-xs text-muted hover:text-dark transition
                       focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-400 rounded"
                aria-label="Share this product"
                (click)="shareProduct()"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z"/>
                </svg>
                Share
              </button>
              @if (shareCopied()) {
                <span class="text-xs text-green-600 font-medium" role="status" aria-live="polite">Link copied!</span>
              }
            </div>

            <!-- Deferred sections — only render when scrolled into view (PDP-7) -->
            @defer (on viewport) {
              <app-product-description [description]="product()!.description" />
            } @placeholder {
              <app-skeleton-loader height="80px" />
            }

            @defer (on viewport) {
              <app-product-reviews
                [productId]="product()!.id"
                [overallRating]="product()!.rating"
              />
            } @placeholder {
              <app-skeleton-loader height="120px" />
            }
          </div>
        </div>

        <!-- Related products — deferred until in viewport (PDP-6 + PDP-7) -->
        @defer (on viewport) {
          <div class="mt-10">
            <app-related-products [currentProductId]="product()!.id" />
          </div>
        } @placeholder {
          <div class="mt-10">
            <app-skeleton-loader height="280px" />
          </div>
        }

        <!-- Recently viewed strip (PDP-9) -->
        @if (recentlyViewed().length > 1) {
          @defer (on viewport) {
            <div class="mt-10 border-t border-gray-100 pt-6">
              <app-section-header
                eyebrow="Your History"
                title="Recently Viewed"
              />
              <div
                class="flex gap-4 overflow-x-auto pb-2 scrollbar-hide snap-x snap-mandatory"
                role="list"
                aria-label="Recently viewed products"
              >
                @for (p of recentlyViewed(); track p.id) {
                  @if (p.id !== product()!.id) {
                    <div class="flex-shrink-0 w-44 snap-start" role="listitem">
                      <app-product-card
                        [product]="p"
                        [isWishlisted]="wishlistIds().includes(p.id)"
                      />
                    </div>
                  }
                }
              </div>
            </div>
          } @placeholder {
            <app-skeleton-loader height="200px" cssClass="mt-10" />
          }
        }

        <!-- Sticky mobile ATC bar — DESIGN.md §5.3, §7 Mobile -->
        <div
          class="fixed bottom-0 inset-x-0 md:hidden z-40
                 bg-white border-t border-border px-4 py-3 shadow-xl"
          aria-label="Purchase actions"
        >
          <app-add-to-cart-panel
            [product]="product()!"
            [selectedSize]="selectedSize()"
            [selectedColour]="selectedColour()"
            [requiresSize]="hasNonOneSize()"
            [requiresColour]="availableColours().length > 0"
            [quantity]="quantity()"
            [variantStock]="selectedVariant()?.stockQuantity ?? null"
            (quantityChange)="quantity.set($event)"
          />
        </div>

      } @else {
        <div class="flex flex-col items-center justify-center py-24">
          <span class="text-6xl mb-4" aria-hidden="true">🔍</span>
          <h2 class="text-xl font-semibold text-dark mb-2">Product not found</h2>
          <p class="text-muted text-sm">This product may have been removed or the link is incorrect.</p>
        </div>
      }
    </div>

    <!-- Size guide modal (PDP-9) -->
    @if (showSizeGuide()) {
      <app-size-guide-modal (close)="showSizeGuide.set(false)" />
    }
  `,
})
export class PdpComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly store      = inject(Store);
  private readonly route      = inject(ActivatedRoute);
  private readonly titleSvc   = inject(Title);
  private readonly metaSvc    = inject(Meta);
  @Inject(DOCUMENT) private readonly doc = inject(DOCUMENT);
  @Inject(PLATFORM_ID) private readonly platformId = inject(PLATFORM_ID);

  private routeSub?: Subscription;
  private intersectionObserver?: IntersectionObserver;

  /** ViewChild refs for IntersectionObserver + focus management */
  @ViewChild('atcPanel')      atcPanelRef?: ElementRef<HTMLElement>;
  @ViewChild('productHeading') productHeadingRef?: ElementRef<HTMLElement>;

  // ── Store signals ─────────────────────────────────────────────────────────
  readonly product$       = this.store.select(selectSelectedProduct);
  readonly product        = toSignal(this.product$,                              { initialValue: null });
  readonly isLoading      = toSignal(this.store.select(selectPdpLoading),        { initialValue: false });
  readonly recentlyViewed = toSignal(this.store.select(selectRecentlyViewed),    { initialValue: [] as Product[] });
  readonly wishlistIds    = toSignal(this.store.select(selectWishlistIds),       { initialValue: [] as string[] });

  // ── Local UI signals ──────────────────────────────────────────────────────
  readonly quantity        = signal<number>(1);
  readonly selectedSize    = signal<string | null>(null);
  readonly selectedColour  = signal<string | null>(null);
  readonly showStickyBar   = signal<boolean>(false);
  readonly showSizeGuide   = signal<boolean>(false);
  readonly shareCopied     = signal<boolean>(false);

  // ── Computed ──────────────────────────────────────────────────────────────

  /** Unique colour options for the current product. */
  readonly availableColours = computed<ColourOption[]>(() => {
    const p = this.product();
    if (!p) return [];
    const seen = new Set<string>();
    const result: ColourOption[] = [];
    for (const v of p.variants) {
      if (v.colour && !seen.has(v.colour)) {
        seen.add(v.colour);
        result.push({ name: v.colour, hex: getColourHex(v.colour) });
      }
    }
    return result;
  });

  /** True when the product has sizes other than ONE SIZE. */
  readonly hasNonOneSize = computed<boolean>(() => {
    const p = this.product();
    if (!p) return false;
    return p.variants.some((v) => v.size !== 'ONE SIZE');
  });

  /** The variant matching the current size + colour selection. */
  readonly selectedVariant = computed<ProductVariant | null>(() => {
    const p = this.product();
    if (!p) return null;
    const size   = this.selectedSize();
    const colour = this.selectedColour();
    return (
      p.variants.find(
        (v) =>
          (size   === null || v.size   === size) &&
          (colour === null || v.colour === colour),
      ) ?? null
    );
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Reactive route param — handles navigation between PDPs without component re-creation
    this.routeSub = this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id') ?? '';
          if (id) {
            this.store.dispatch(CatalogActions.loadProduct({ id }));
            this.selectedSize.set(null);
            this.selectedColour.set(null);
            this.quantity.set(1);
            this.showStickyBar.set(false);
          }
          return this.product$;
        }),
      )
      .subscribe((product) => {
        if (product) {
          // Auto-select first available (in-stock) variant on product load
          if (this.selectedSize() === null) {
            this.autoSelectFirstVariant(product);
          }
          // PDP-8: Update page title, meta, OG tags, JSON-LD
          this.updateSeoTags(product);
          // PDP-8: Move focus to product heading for screen reader announcement
          setTimeout(() => this.productHeadingRef?.nativeElement.focus(), 100);
        }
      });
  }

  ngAfterViewInit(): void {
    // PDP-9: IntersectionObserver — show sticky bar when ATC panel leaves viewport
    if (isPlatformBrowser(this.platformId) && this.atcPanelRef) {
      this.intersectionObserver = new IntersectionObserver(
        ([entry]) => this.showStickyBar.set(!entry.isIntersecting),
        { threshold: 0 },
      );
      this.intersectionObserver.observe(this.atcPanelRef.nativeElement);
    }
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.intersectionObserver?.disconnect();
    // Clear stale product so next PDP visit doesn't flash previous product
    this.store.dispatch(CatalogActions.clearSelectedProduct());
    // Clear reviews so next PDP visit starts fresh
    this.store.dispatch(CatalogActions.clearReviews());
    // Remove JSON-LD script tag
    this.removeJsonLd();
  }

  // ── Event handlers ────────────────────────────────────────────────────────

  onSizeChange(size: string): void {
    this.selectedSize.set(size);
    // If the currently selected colour is OOS for the new size, clear it
    const colour = this.selectedColour();
    if (colour) {
      const p = this.product();
      if (p) {
        const stillAvailable = p.variants.some(
          (v) => v.size === size && v.colour === colour && v.stockQuantity > 0,
        );
        if (!stillAvailable) this.selectedColour.set(null);
      }
    }
  }

  shareProduct(): void {
    const url = isPlatformBrowser(this.platformId) ? window.location.href : '';
    if (navigator.share) {
      const p = this.product();
      navigator.share({
        title: p?.name ?? 'Product',
        text:  p ? `${p.brandName} — ${p.name}` : '',
        url,
      }).catch(() => { /* user cancelled */ });
    } else {
      // Fallback: copy to clipboard
      navigator.clipboard.writeText(url).then(() => {
        this.shareCopied.set(true);
        setTimeout(() => this.shareCopied.set(false), 2500);
      });
    }
  }

  // ── SEO helpers (PDP-8) ───────────────────────────────────────────────────

  private updateSeoTags(product: Product): void {
    const title       = `${product.name} — ${product.brandName} | StyleNest`;
    const description = product.description?.slice(0, 155) ?? `${product.brandName} ${product.name}`;
    const image       = product.imageUrls[0] ?? '';
    const url         = isPlatformBrowser(this.platformId) ? window.location.href : '';

    // Page title
    this.titleSvc.setTitle(title);

    // Standard meta
    this.metaSvc.updateTag({ name: 'description', content: description });

    // Open Graph
    this.metaSvc.updateTag({ property: 'og:title',       content: title });
    this.metaSvc.updateTag({ property: 'og:description', content: description });
    this.metaSvc.updateTag({ property: 'og:image',       content: image });
    this.metaSvc.updateTag({ property: 'og:type',        content: 'product' });
    this.metaSvc.updateTag({ property: 'og:url',         content: url });

    // Twitter card
    this.metaSvc.updateTag({ name: 'twitter:card',        content: 'summary_large_image' });
    this.metaSvc.updateTag({ name: 'twitter:title',       content: title });
    this.metaSvc.updateTag({ name: 'twitter:description', content: description });
    this.metaSvc.updateTag({ name: 'twitter:image',       content: image });

    // Canonical URL
    this.upsertLinkTag('canonical', url);

    // JSON-LD Product schema
    this.injectJsonLd({
      '@context':   'https://schema.org',
      '@type':      'Product',
      name:         product.name,
      description:  product.description,
      image:        product.imageUrls,
      brand:        { '@type': 'Brand', name: product.brandName },
      offers: {
        '@type':       'Offer',
        price:         product.salePrice ?? product.price,
        priceCurrency: 'INR',
        availability:  product.inStock
          ? 'https://schema.org/InStock'
          : 'https://schema.org/OutOfStock',
        url,
      },
      aggregateRating: product.reviewCount > 0
        ? {
            '@type':      'AggregateRating',
            ratingValue:  product.rating,
            reviewCount:  product.reviewCount,
          }
        : undefined,
    });
  }

  private upsertLinkTag(rel: string, href: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    let link = this.doc.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`);
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', rel);
      this.doc.head.appendChild(link);
    }
    link.setAttribute('href', href);
  }

  private injectJsonLd(schema: object): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.removeJsonLd();
    const script = this.doc.createElement('script');
    script.type = 'application/ld+json';
    script.id   = 'pdp-json-ld';
    script.text = JSON.stringify(schema);
    this.doc.head.appendChild(script);
  }

  private removeJsonLd(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.doc.getElementById('pdp-json-ld')?.remove();
  }

  // ── Private helpers ───────────────────────────────────────────────────────

  private autoSelectFirstVariant(product: Product): void {
    const firstInStock = product.variants.find((v) => v.stockQuantity > 0);
    if (!firstInStock) return;
    if (firstInStock.size !== 'ONE SIZE') this.selectedSize.set(firstInStock.size);
    if (firstInStock.colour) this.selectedColour.set(firstInStock.colour);
  }
}
