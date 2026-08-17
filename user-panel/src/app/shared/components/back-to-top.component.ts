import { ChangeDetectionStrategy, Component, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-back-to-top',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- DESIGN.md §4.19 Back-to-Top — fixed, appears after 400px scroll -->
    @if (visible()) {
      <button
        class="fixed bottom-20 md:bottom-6 right-4 md:right-6 z-50
               w-12 h-12 rounded-full bg-navy hover:bg-red text-white
               flex items-center justify-center shadow-lg
               transition-all duration-300 ease-out
               focus-visible:ring-2 focus-visible:ring-red focus-visible:ring-offset-2
               back-to-top-reveal"
        aria-label="Back to top"
        (click)="scrollToTop()"
      >
        <!-- ChevronUp icon -->
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 15l7-7 7 7"/>
        </svg>
      </button>
    }
  `,
  styles: [`
    .back-to-top-reveal {
      animation: back-to-top-appear 300ms ease-out forwards;
    }
    @keyframes back-to-top-appear {
      from { opacity: 0; transform: translateY(8px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `],
})
export class BackToTopComponent {
  readonly visible = signal(false);

  @HostListener('window:scroll')
  onScroll(): void {
    this.visible.set(window.scrollY > 400);
  }

  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
