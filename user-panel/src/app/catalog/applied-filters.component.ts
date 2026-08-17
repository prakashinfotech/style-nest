import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ActiveFilter {
  key: string;
  label: string;
  value: string;
}

@Component({
  selector: 'app-applied-filters',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    @if (filters.length > 0) {
      <div class="flex flex-wrap gap-2 items-center" aria-label="Applied filters">
        <span class="text-xs text-muted font-medium">Filters:</span>
        @for (filter of filters; track filter.key + filter.value) {
          <span class="inline-flex items-center gap-1 bg-navy/10 text-navy text-xs px-2.5 py-1 rounded-full">
            {{ filter.label }}
            <button
              class="ml-0.5 hover:text-red transition"
              [attr.aria-label]="'Remove ' + filter.label + ' filter'"
              (click)="remove.emit(filter)"
            >
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </span>
        }
        <button class="text-xs text-red hover:underline ml-1" (click)="clearAll.emit()">
          Clear all
        </button>
      </div>
    }
  `,
})
export class AppliedFiltersComponent {
  @Input({ required: true }) filters: ActiveFilter[] = [];
  @Output() remove   = new EventEmitter<ActiveFilter>();
  @Output() clearAll = new EventEmitter<void>();
}
