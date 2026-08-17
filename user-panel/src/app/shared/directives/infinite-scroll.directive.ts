/**
 * ENH-CAT-005 — Infinite Scroll Directive.
 *
 * Attach to any sentinel element (e.g. an empty div at the bottom of a list).
 * When the element enters the viewport (with a 100px bottom rootMargin lookahead),
 * emits a `(scrolled)` event so the host can request the next page.
 *
 * Usage:
 *   <div appInfiniteScroll (scrolled)="loadMore()"></div>
 */

import {
  AfterViewInit,
  Directive,
  ElementRef,
  EventEmitter,
  NgZone,
  OnDestroy,
  Output,
} from '@angular/core';

@Directive({
  selector: '[appInfiniteScroll]',
  standalone: true,
})
export class InfiniteScrollDirective implements AfterViewInit, OnDestroy {
  /** Fires when the sentinel element enters the viewport. */
  @Output() scrolled = new EventEmitter<void>();

  private observer: IntersectionObserver | null = null;

  constructor(
    private readonly el: ElementRef<HTMLElement>,
    private readonly ngZone: NgZone,
  ) {}

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          // Re-enter Angular zone so change detection picks up signal/state updates
          this.ngZone.run(() => this.scrolled.emit());
        }
      },
      { threshold: 0.1, rootMargin: '0px 0px 100px 0px' },
    );
    this.observer.observe(this.el.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.observer = null;
  }
}
