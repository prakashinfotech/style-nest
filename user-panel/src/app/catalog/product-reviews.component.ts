import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { StarRatingComponent } from '../shared/components/star-rating.component';
import { ReviewFormComponent } from './review-form.component';
import { ReviewPhotoLightboxComponent } from './review-photo-lightbox.component';
import { Review, CreateReviewRequest } from '../core/models/review.model';
import { CatalogActions } from '../store/catalog/catalog.actions';
import {
  selectReviews,
  selectReviewsTotalCount,
  selectReviewsLoading,
  selectReviewsPage,
  selectHasMoreReviews,
  selectPostingReview,
  selectPostReviewError,
} from '../store/catalog/catalog.selectors';
import { selectIsLoggedIn } from '../store/auth/auth.selectors';

@Component({
  selector: 'app-product-reviews',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, StarRatingComponent, ReviewFormComponent, ReviewPhotoLightboxComponent],
  template: `
    <section class="border-t border-gray-100 pt-6" aria-labelledby="reviews-heading">

      <!-- Header row -->
      <div class="flex items-center justify-between mb-5">
        <h2 id="reviews-heading" class="text-base font-semibold text-dark">
          Customer Reviews
          @if (totalCount() > 0) {
            <span class="text-muted font-normal ml-1">({{ totalCount() }})</span>
          }
        </h2>
        @if (overallRating > 0) {
          <app-star-rating [rating]="overallRating" [showCount]="false" />
        }
      </div>

      <!-- Star distribution breakdown -->
      @if (totalCount() > 0 && overallRating > 0) {
        <div class="mb-6 p-4 bg-gray-50 rounded-lg flex flex-col sm:flex-row gap-4 items-start sm:items-center">
          <!-- Overall score -->
          <div class="flex flex-col items-center min-w-[72px]">
            <span class="text-4xl font-bold text-dark leading-none">{{ overallRating | number:'1.1-1' }}</span>
            <app-star-rating [rating]="overallRating" [showCount]="false" class="mt-1" />
            <span class="text-xs text-muted mt-1">{{ totalCount() }} reviews</span>
          </div>
          <!-- Bar chart -->
          <div class="flex-1 w-full space-y-1.5" aria-label="Rating distribution">
            @for (bar of ratingBars(); track bar.star) {
              <div class="flex items-center gap-2 text-xs">
                <span class="w-3 text-right text-muted">{{ bar.star }}</span>
                <span class="text-yellow-400 text-sm leading-none" aria-hidden="true">★</span>
                <div class="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden">
                  <div
                    class="h-full bg-yellow-400 rounded-full transition-all duration-500"
                    [style.width.%]="bar.pct"
                    [attr.aria-label]="bar.star + ' stars: ' + bar.pct + '%'"
                  ></div>
                </div>
                <span class="w-8 text-muted text-right">{{ bar.pct }}%</span>
              </div>
            }
          </div>
        </div>
      }

      <!-- Write a review button / form -->
      @if (isLoggedIn()) {
        @if (!showForm()) {
          <button
            type="button"
            (click)="showForm.set(true)"
            class="mb-5 px-4 py-2 border border-red-600 text-red-600 text-sm font-medium rounded
                   hover:bg-red-50 transition-colors
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"
          >
            Write a Review
          </button>
        } @else {
          <div class="mb-6">
            <app-review-form
              [submitting]="postingReview()"
              [submitError]="postReviewError()"
              (submitted)="onReviewSubmit($event)"
              (cancelled)="showForm.set(false)"
            />
          </div>
        }
      }

      <!-- Review list -->
      @if (reviewsLoading() && reviews().length === 0) {
        <!-- Initial load skeleton -->
        <ul class="space-y-5" aria-busy="true" aria-label="Loading reviews">
          @for (i of [1,2,3]; track i) {
            <li class="border-b border-gray-50 pb-5 animate-pulse">
              <div class="h-3 bg-gray-200 rounded w-1/4 mb-2"></div>
              <div class="h-2 bg-gray-200 rounded w-1/3 mb-3"></div>
              <div class="h-2 bg-gray-200 rounded w-3/4 mb-1"></div>
              <div class="h-2 bg-gray-200 rounded w-1/2"></div>
            </li>
          }
        </ul>
      } @else if (reviews().length === 0) {
        <p class="text-sm text-muted py-4">
          No reviews yet.
          @if (!isLoggedIn()) { Log in to be the first to review this product. }
          @else { Be the first to review this product. }
        </p>
      } @else {
        <ul class="space-y-5" aria-label="Customer reviews">
          @for (review of reviews(); track review.id) {
            <li class="border-b border-gray-50 pb-5 last:border-0">
              <div class="flex items-start justify-between gap-3 mb-1">
                <div>
                  <p class="text-sm font-semibold text-dark">{{ review.author }}</p>
                  <app-star-rating [rating]="review.rating" [showCount]="false" />
                </div>
                <time
                  class="text-xs text-muted flex-shrink-0"
                  [attr.datetime]="review.date"
                >
                  {{ review.date | date:'mediumDate' }}
                </time>
              </div>
              <p class="text-sm font-medium text-dark mt-1">{{ review.title }}</p>
              <p class="text-sm text-dark/80 mt-1 leading-relaxed">{{ review.body }}</p>

              <!-- ENH-PDP-008 — Photo thumbnails strip -->
              @if (review.photoUrls && review.photoUrls.length > 0) {
                <div class="flex gap-2 mt-3 flex-wrap" role="list" aria-label="Review photos">
                  @for (photo of review.photoUrls; track photo; let pi = $index) {
                    <button
                      role="listitem"
                      type="button"
                      [attr.aria-label]="'View photo ' + (pi + 1) + ' of ' + review.photoUrls!.length"
                      class="w-16 h-16 rounded overflow-hidden border border-border flex-shrink-0
                             hover:border-navy transition focus-visible:outline-none
                             focus-visible:ring-2 focus-visible:ring-navy"
                      (click)="openLightbox(review.photoUrls!, pi)"
                    >
                      <img
                        [src]="photo"
                        [alt]="'Review photo ' + (pi + 1)"
                        class="w-full h-full object-cover"
                        loading="lazy"
                      />
                    </button>
                  }
                </div>
              }
            </li>
          }
        </ul>

        <!-- Load more -->
        @if (hasMore()) {
          <div class="mt-5 text-center">
            <button
              type="button"
              [disabled]="reviewsLoading()"
              (click)="loadMore()"
              class="px-6 py-2 border border-border text-sm font-medium rounded
                     text-dark hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed
                     transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-400"
            >
              @if (reviewsLoading()) { Loading… } @else { Load More Reviews }
            </button>
          </div>
        }
      }
    </section>

    <!-- ENH-PDP-008 — Review photo lightbox -->
    @if (lightboxPhotos()) {
      <app-review-photo-lightbox
        [photos]="lightboxPhotos()!"
        [initialIndex]="lightboxIndex()"
        (close)="closeLightbox()"
      />
    }
  `,
})
export class ProductReviewsComponent implements OnChanges {
  @Input({ required: true }) productId!: string;
  @Input() overallRating = 0;

  private readonly store = inject(Store);

  readonly reviews        = toSignal(this.store.select(selectReviews),           { initialValue: [] as Review[] });
  readonly totalCount     = toSignal(this.store.select(selectReviewsTotalCount),  { initialValue: 0 });
  readonly reviewsLoading = toSignal(this.store.select(selectReviewsLoading),     { initialValue: false });
  readonly currentPage    = toSignal(this.store.select(selectReviewsPage),        { initialValue: 1 });
  readonly hasMore        = toSignal(this.store.select(selectHasMoreReviews),     { initialValue: false });
  readonly postingReview  = toSignal(this.store.select(selectPostingReview),      { initialValue: false });
  readonly postReviewError = toSignal(this.store.select(selectPostReviewError),   { initialValue: null as string | null });
  readonly isLoggedIn     = toSignal(this.store.select(selectIsLoggedIn),         { initialValue: false });

  readonly showForm = signal(false);

  // ENH-PDP-008 — lightbox state
  readonly lightboxPhotos = signal<string[] | null>(null);
  readonly lightboxIndex  = signal(0);

  /** Computed star distribution bars (5 → 1) based on loaded reviews. */
  ratingBars(): Array<{ star: number; pct: number }> {
    const all = this.reviews();
    if (all.length === 0) return [];
    return [5, 4, 3, 2, 1].map((star) => {
      const count = all.filter((r) => r.rating === star).length;
      return { star, pct: Math.round((count / all.length) * 100) };
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When productId changes (navigating between PDPs), clear and reload
    if (changes['productId'] && this.productId) {
      this.store.dispatch(CatalogActions.clearReviews());
      this.showForm.set(false);
    }
  }

  loadMore(): void {
    const nextPage = (this.currentPage() ?? 1) + 1;
    this.store.dispatch(
      CatalogActions.loadReviews({ productId: this.productId, page: nextPage }),
    );
  }

  // ENH-PDP-008 — lightbox helpers
  openLightbox(photos: string[], index: number): void {
    this.lightboxPhotos.set(photos);
    this.lightboxIndex.set(index);
  }

  closeLightbox(): void {
    this.lightboxPhotos.set(null);
  }

  onReviewSubmit(request: CreateReviewRequest): void {
    this.store.dispatch(
      CatalogActions.postReview({ productId: this.productId, request }),
    );
    // Hide form on success — effect will prepend the new review
    // We watch postingReview going false to close the form
    this.showForm.set(false);
  }
}
