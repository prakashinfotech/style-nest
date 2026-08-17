import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductFilters } from '../core/models/product.model';

type SortOption = ProductFilters['sort'];

interface SortItem { value: SortOption; label: string; }

@Component({
  selector: 'app-sort-dropdown',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="flex items-center gap-2">
      <label class="text-xs text-muted whitespace-nowrap" for="sort-select">Sort by:</label>
      <select
        id="sort-select"
        [ngModel]="currentSort"
        class="border border-gray-200 rounded px-2 py-1.5 text-sm bg-card text-dark outline-none focus:border-navy cursor-pointer"
        (ngModelChange)="sortChange.emit($event)"
      >
        @for (opt of options; track opt.value) {
          <option [value]="opt.value">{{ opt.label }}</option>
        }
      </select>
    </div>
  `,
})
export class SortDropdownComponent {
  @Input() currentSort: SortOption = null;
  @Output() sortChange = new EventEmitter<SortOption>();

  readonly options: SortItem[] = [
    { value: null,         label: 'Relevance' },
    { value: 'newest',     label: 'Newest First' },
    { value: 'price_asc',  label: 'Price: Low to High' },
    { value: 'price_desc', label: 'Price: High to Low' },
    { value: 'rating',     label: 'Avg. Customer Rating' },
  ];
}
