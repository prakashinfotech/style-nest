import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="flex flex-col items-center justify-center py-16 px-4 text-center">
      <span class="text-6xl mb-5" role="img" [attr.aria-label]="title">{{ icon }}</span>
      <h3 class="text-lg font-semibold text-dark mb-2">{{ title }}</h3>
      <p class="text-muted text-sm mb-6 max-w-xs">{{ subtitle }}</p>
      @if (ctaLabel) {
        @if (ctaRoute) {
          <a
            [routerLink]="ctaRoute"
            class="bg-navy text-white px-7 py-2.5 rounded-lg text-sm font-semibold hover:bg-blue transition"
          >{{ ctaLabel }}</a>
        } @else {
          <button
            class="bg-navy text-white px-7 py-2.5 rounded-lg text-sm font-semibold hover:bg-blue transition"
            (click)="ctaClick.emit()"
          >{{ ctaLabel }}</button>
        }
      }
    </div>
  `,
})
export class EmptyStateComponent {
  @Input({ required: true }) icon!: string;
  @Input({ required: true }) title!: string;
  @Input({ required: true }) subtitle!: string;
  @Input() ctaLabel?: string;
  @Input() ctaRoute?: string;
  @Output() readonly ctaClick = new EventEmitter<void>();
}
