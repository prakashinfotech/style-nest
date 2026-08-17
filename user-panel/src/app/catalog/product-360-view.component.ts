/**
 * ENH-PDP-007 — 360-View Product Gallery.
 *
 * Renders a drag-to-spin / swipe-to-spin image carousel using the product's
 * image URLs as sequential rotation frames.
 *
 * Interaction model:
 *   - Desktop: click-and-drag horizontally to rotate
 *   - Mobile:  touch-swipe horizontally to rotate
 *   - Keyboard: ArrowLeft / ArrowRight to step through frames
 *
 * The active frame index advances proportionally to the drag distance so
 * the rotation feels physical (1 full drag width ≈ one full revolution).
 *
 * Accessibility:
 *   - role="img" with aria-label on the container
 *   - aria-live="polite" announces frame changes to screen readers
 *   - A static hint text "Drag to rotate" is visible until first interaction
 */

import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  Input,
  OnDestroy,
  ViewChild,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-product-360-view',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- 360° drag-to-spin container -->
    <div
      #spinContainer
      class="relative aspect-[3/4] rounded-lg overflow-hidden bg-gray-50 select-none cursor-grab"
      [class.cursor-grabbing]="isDragging()"
      role="img"
      tabindex="0"
      [attr.aria-label]="productName + ' — 360° view, frame ' + (activeFrame() + 1) + ' of ' + frames.length"
      (mousedown)="onMouseDown($event)"
      (touchstart)="onTouchStart($event)"
      (keydown.ArrowLeft)="step(-1)"
      (keydown.ArrowRight)="step(1)"
    >
      <!-- Frames (only active frame is visible) -->
      @for (frame of frames; track frame; let i = $index) {
        <img
          [src]="frame"
          [alt]="productName + ' 360° frame ' + (i + 1)"
          class="absolute inset-0 w-full h-full object-contain transition-opacity duration-75"
          [class.opacity-100]="activeFrame() === i"
          [class.opacity-0]="activeFrame() !== i"
          [loading]="i === 0 ? 'eager' : 'lazy'"
          draggable="false"
        />
      }

      <!-- Drag hint — fades out after first interaction -->
      @if (!hasInteracted()) {
        <div
          class="absolute bottom-4 inset-x-0 flex items-center justify-center pointer-events-none"
          aria-hidden="true"
        >
          <div class="flex items-center gap-1.5 bg-black/50 text-white text-xs font-medium
                      px-3 py-1.5 rounded-full">
            <!-- Rotate icon -->
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
            Drag to rotate
          </div>
        </div>
      }

      <!-- Frame counter pill -->
      @if (frames.length > 1) {
        <div
          class="absolute top-3 right-3 bg-black/60 text-white text-xs font-medium
                 px-2 py-0.5 rounded-full pointer-events-none"
          aria-hidden="true"
        >
          360°
        </div>
      }

      <!-- Screen-reader live region for frame changes -->
      <div
        class="sr-only"
        aria-live="polite"
        aria-atomic="true"
      >Frame {{ activeFrame() + 1 }} of {{ frames.length }}</div>
    </div>
  `,
})
export class Product360ViewComponent implements OnDestroy {
  @Input({ required: true }) frames: string[] = [];
  @Input() productName = '';

  @ViewChild('spinContainer') containerRef?: ElementRef<HTMLElement>;

  readonly activeFrame   = signal(0);
  readonly isDragging    = signal(false);
  readonly hasInteracted = signal(false);

  private dragStartX      = 0;
  private dragStartFrame  = 0;

  // ── Mouse drag ────────────────────────────────────────────────────────────

  onMouseDown(e: MouseEvent): void {
    e.preventDefault();
    this.startDrag(e.clientX);
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent): void {
    if (!this.isDragging()) return;
    this.updateFrame(e.clientX);
  }

  @HostListener('document:mouseup')
  onMouseUp(): void {
    this.isDragging.set(false);
  }

  // ── Touch swipe ───────────────────────────────────────────────────────────

  onTouchStart(e: TouchEvent): void {
    this.startDrag(e.touches[0].clientX);
  }

  @HostListener('document:touchmove', ['$event'])
  onTouchMove(e: TouchEvent): void {
    if (!this.isDragging()) return;
    this.updateFrame(e.touches[0].clientX);
  }

  @HostListener('document:touchend')
  onTouchEnd(): void {
    this.isDragging.set(false);
  }

  // ── Keyboard ──────────────────────────────────────────────────────────────

  step(delta: number): void {
    const n = this.frames.length;
    if (n === 0) return;
    this.hasInteracted.set(true);
    this.activeFrame.update((i) => (i + delta + n) % n);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnDestroy(): void {
    this.isDragging.set(false);
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private startDrag(clientX: number): void {
    if (this.frames.length === 0) return;
    this.hasInteracted.set(true);
    this.isDragging.set(true);
    this.dragStartX     = clientX;
    this.dragStartFrame = this.activeFrame();
  }

  private updateFrame(clientX: number): void {
    const n = this.frames.length;
    if (n === 0) return;

    const containerWidth =
      this.containerRef?.nativeElement.getBoundingClientRect().width ?? 300;

    // One full drag across the container = one full revolution.
    const dragDelta = clientX - this.dragStartX;
    const frameDelta = Math.round((dragDelta / containerWidth) * n);
    const newFrame   = ((this.dragStartFrame - frameDelta) % n + n) % n;

    this.activeFrame.set(newFrame);
  }
}
