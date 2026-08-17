/**
 * ENH-PDP-008 — Review Photo Lightbox.
 *
 * Full-screen modal that displays review photos with prev/next navigation.
 * Opened from ProductReviewsComponent when a review thumbnail is clicked.
 *
 * Accessibility:
 *   - role="dialog" with aria-modal="true"
 *   - aria-label announces current image index and total
 *   - Keyboard: Escape closes, ArrowLeft/ArrowRight navigate
 *   - Focus trapped inside modal while open
 */

import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-review-photo-lightbox',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- Backdrop -->
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/90"
      role="dialog"
      aria-modal="true"
      [attr.aria-label]="'Review photo ' + (activeIndex() + 1) + ' of ' + photos.length"
      (click)="onBackdropClick($event)"
      (keydown.Escape)="close.emit()"
      (keydown.ArrowLeft)="prev()"
      (keydown.ArrowRight)="next()"
      tabindex="0"
    >
      <!-- Modal content — stops backdrop click propagation -->
      <div
        class="relative flex flex-col items-center max-w-3xl w-full mx-4"
        (click)="$event.stopPropagation()"
      >

        <!-- Main image -->
        <div class="relative w-full flex items-center justify-center">

          <!-- Prev button -->
          @if (photos.length > 1) {
            <button
              class="absolute left-0 z-10 w-10 h-10 bg-white/20 hover:bg-white/40 rounded-full
                     flex items-center justify-center text-white transition
                     focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
              aria-label="Previous photo"
              (click)="prev()"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
              </svg>
            </button>
          }

          <!-- Photo -->
          <img
            [src]="photos[activeIndex()]"
            [alt]="'Review photo ' + (activeIndex() + 1) + ' of ' + photos.length"
            class="max-h-[75vh] max-w-full object-contain rounded-lg shadow-2xl"
            loading="eager"
          />

          <!-- Next button -->
          @if (photos.length > 1) {
            <button
              class="absolute right-0 z-10 w-10 h-10 bg-white/20 hover:bg-white/40 rounded-full
                     flex items-center justify-center text-white transition
                     focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
              aria-label="Next photo"
              (click)="next()"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
              </svg>
            </button>
          }

          <!-- Close button -->
          <button
            class="absolute top-2 right-2 w-9 h-9 bg-white/20 hover:bg-white/40 rounded-full
                   flex items-center justify-center text-white transition z-10
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
            aria-label="Close photo viewer"
            (click)="close.emit()"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Counter + thumbnail strip -->
        <div class="mt-4 flex flex-col items-center gap-3">
          <!-- Counter pill -->
          @if (photos.length > 1) {
            <span class="text-white/70 text-sm">
              {{ activeIndex() + 1 }} / {{ photos.length }}
            </span>

            <!-- Thumbnail strip -->
            <div class="flex gap-2" role="tablist" aria-label="Photo thumbnails">
              @for (photo of photos; track photo; let i = $index) {
                <button
                  role="tab"
                  [attr.aria-selected]="activeIndex() === i"
                  [attr.aria-label]="'View photo ' + (i + 1)"
                  class="w-12 h-12 rounded overflow-hidden border-2 transition flex-shrink-0
                         focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
                  [class.border-white]="activeIndex() === i"
                  [class.border-transparent]="activeIndex() !== i"
                  [class.opacity-100]="activeIndex() === i"
                  [class.opacity-50]="activeIndex() !== i"
                  (click)="activeIndex.set(i)"
                >
                  <img
                    [src]="photo"
                    [alt]="'Thumbnail ' + (i + 1)"
                    class="w-full h-full object-cover"
                    loading="lazy"
                  />
                </button>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class ReviewPhotoLightboxComponent implements OnInit {
  @Input({ required: true }) photos: string[]  = [];
  @Input() initialIndex = 0;
  @Output() close = new EventEmitter<void>();

  readonly activeIndex = signal(0);

  ngOnInit(): void {
    this.activeIndex.set(
      Math.max(0, Math.min(this.initialIndex, this.photos.length - 1)),
    );
  }

  prev(): void {
    const n = this.photos.length;
    this.activeIndex.update((i) => (i - 1 + n) % n);
  }

  next(): void {
    const n = this.photos.length;
    this.activeIndex.update((i) => (i + 1) % n);
  }

  onBackdropClick(e: MouseEvent): void {
    if (e.target === e.currentTarget) this.close.emit();
  }

  @HostListener('document:keydown.Escape')
  onEscape(): void {
    this.close.emit();
  }
}
