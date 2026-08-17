import { ChangeDetectionStrategy, Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-product-description',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <section class="border-t border-gray-100 pt-6">
      <button
        class="flex items-center justify-between w-full text-left"
        [attr.aria-expanded]="expanded()"
        (click)="expanded.update((v) => !v)"
      >
        <h2 class="text-base font-semibold text-dark">Product Description</h2>
        <svg
          class="w-4 h-4 text-muted transition-transform"
          [class.rotate-180]="expanded()"
          fill="none" stroke="currentColor" viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
      </button>

      @if (expanded()) {
        <div class="mt-4 text-sm text-dark/80 leading-relaxed prose-sm">
          <p>{{ description }}</p>
        </div>
      }
    </section>
  `,
})
export class ProductDescriptionComponent {
  @Input({ required: true }) description!: string;
  readonly expanded = signal(true);
}
