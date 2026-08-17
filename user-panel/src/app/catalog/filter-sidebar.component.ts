import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Category } from '../core/models/product.model';
import { AttributeDefinition } from '../core/services/catalog.service';

export interface FilterState {
  categoryId: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  selectedBrandIds: string[];
  minDiscount: number | null;
  selectedAttributes: Record<string, string[]>;
}

interface Brand { id: string; name: string; }

@Component({
  selector: 'app-filter-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <aside class="bg-card rounded-lg p-4 text-sm" aria-label="Filters">
      <div class="flex items-center justify-between mb-4">
        <h2 class="font-bold text-dark text-base">Filters</h2>
        <button class="text-blue text-xs hover:underline" (click)="clearAll()">Clear All</button>
      </div>

      <!-- Category -->
      @if (categories.length > 0) {
        <section class="mb-5 border-b border-gray-100 pb-5">
          <h3 class="font-semibold text-dark mb-3">Category</h3>
          <ul class="space-y-2">
            @for (cat of categories; track cat.id) {
              <li>
                <label class="flex items-center gap-2 cursor-pointer hover:text-navy">
                  <input
                    type="radio"
                    name="category"
                    [value]="cat.id"
                    [checked]="currentFilters.categoryId === cat.id"
                    class="accent-navy"
                    (change)="onCategoryChange(cat.id)"
                  />
                  {{ cat.name }}
                </label>
              </li>
            }
          </ul>
        </section>
      }

      <!-- Price range -->
      <section class="mb-5 border-b border-gray-100 pb-5">
        <h3 class="font-semibold text-dark mb-3">Price Range</h3>
        <div class="flex gap-2">
          <input
            type="number"
            placeholder="Min ₹"
            [ngModel]="currentFilters.minPrice"
            class="w-full border border-gray-200 rounded px-2 py-1.5 text-xs outline-none focus:border-navy"
            (ngModelChange)="onMinPriceChange($event)"
          />
          <input
            type="number"
            placeholder="Max ₹"
            [ngModel]="currentFilters.maxPrice"
            class="w-full border border-gray-200 rounded px-2 py-1.5 text-xs outline-none focus:border-navy"
            (ngModelChange)="onMaxPriceChange($event)"
          />
        </div>
        <div class="mt-2 flex gap-2 flex-wrap">
          @for (range of priceRanges; track range.label) {
            <button
              class="text-xs border border-gray-200 rounded-full px-2 py-0.5 hover:border-navy hover:text-navy transition"
              (click)="applyPriceRange(range.min, range.max)"
            >{{ range.label }}</button>
          }
        </div>
      </section>

      <!-- Discount -->
      <section class="mb-5 border-b border-gray-100 pb-5">
        <h3 class="font-semibold text-dark mb-3">Discount</h3>
        <ul class="space-y-2">
          @for (opt of discountOptions; track opt.value) {
            <li>
              <label class="flex items-center gap-2 cursor-pointer hover:text-navy">
                <input
                  type="radio"
                  name="discount"
                  [value]="opt.value"
                  [checked]="currentFilters.minDiscount === opt.value"
                  class="accent-red"
                  (change)="onDiscountChange(opt.value)"
                />
                <span [class.text-red]="currentFilters.minDiscount === opt.value">
                  {{ opt.label }}
                </span>
              </label>
            </li>
          }
          @if (currentFilters.minDiscount !== null) {
            <li>
              <button
                class="text-xs text-blue hover:underline mt-1"
                (click)="onDiscountChange(null)"
              >Clear discount filter</button>
            </li>
          }
        </ul>
      </section>

      <!-- Brands -->
      @if (brands.length > 0) {
        <section class="mb-5 border-b border-gray-100 pb-5">
          <h3 class="font-semibold text-dark mb-3">Brand</h3>
          <ul class="space-y-2 max-h-48 overflow-y-auto">
            @for (brand of brands; track brand.id) {
              <li>
                <label class="flex items-center gap-2 cursor-pointer hover:text-navy">
                  <input
                    type="checkbox"
                    [checked]="currentFilters.selectedBrandIds.includes(brand.id)"
                    class="accent-navy"
                    (change)="onBrandToggle(brand.id)"
                  />
                  {{ brand.name }}
                </label>
              </li>
            }
          </ul>
        </section>
      }

      <!-- Dynamic Attributes (EAV) -->
      @for (attr of attributes; track attr.id) {
        @if (attr.allowedValues.length > 0) {
          <section class="mb-5 border-b border-gray-100 pb-5 last:border-0">
            <h3 class="font-semibold text-dark mb-3">{{ attr.name }}</h3>
            <div class="flex flex-wrap gap-1.5">
              @for (val of attr.allowedValues; track val) {
                <button
                  type="button"
                  class="px-2.5 py-1 rounded-full text-xs border transition-colors"
                  [class.bg-navy]="isAttrSelected(attr.id, val)"
                  [class.text-white]="isAttrSelected(attr.id, val)"
                  [class.border-navy]="isAttrSelected(attr.id, val)"
                  [class.border-border]="!isAttrSelected(attr.id, val)"
                  [class.text-dark]="!isAttrSelected(attr.id, val)"
                  [class.hover:border-navy]="!isAttrSelected(attr.id, val)"
                  (click)="onAttributeToggle(attr.id, val)"
                >{{ val }}</button>
              }
            </div>
          </section>
        }
      }
    </aside>
  `,
})
export class FilterSidebarComponent {
  @Input() categories: Category[] = [];
  @Input() brands: Brand[] = [];
  @Input() attributes: AttributeDefinition[] = [];
  @Input() currentFilters: FilterState = {
    categoryId: null, minPrice: null, maxPrice: null, selectedBrandIds: [], minDiscount: null,
    selectedAttributes: {},
  };
  @Output() filtersChange = new EventEmitter<FilterState>();

  readonly discountOptions: { label: string; value: number }[] = [
    { label: '10% & above',  value: 10 },
    { label: '20% & above',  value: 20 },
    { label: '30% & above',  value: 30 },
    { label: '40% & above',  value: 40 },
    { label: '50% & above',  value: 50 },
  ];

  readonly priceRanges = [
    { label: 'Under ₹500',   min: null, max: 500 },
    { label: '₹500–₹2,000',  min: 500,  max: 2000 },
    { label: '₹2,000–₹5,000',min: 2000, max: 5000 },
    { label: '₹5,000+',      min: 5000, max: null },
  ];

  onCategoryChange(id: string): void {
    this.filtersChange.emit({ ...this.currentFilters, categoryId: id });
  }

  onMinPriceChange(value: number | null): void {
    this.filtersChange.emit({ ...this.currentFilters, minPrice: value });
  }

  onMaxPriceChange(value: number | null): void {
    this.filtersChange.emit({ ...this.currentFilters, maxPrice: value });
  }

  onBrandToggle(id: string): void {
    const ids = this.currentFilters.selectedBrandIds.includes(id)
      ? this.currentFilters.selectedBrandIds.filter((b) => b !== id)
      : [...this.currentFilters.selectedBrandIds, id];
    this.filtersChange.emit({ ...this.currentFilters, selectedBrandIds: ids });
  }

  applyPriceRange(min: number | null, max: number | null): void {
    this.filtersChange.emit({ ...this.currentFilters, minPrice: min, maxPrice: max });
  }

  onDiscountChange(value: number | null): void {
    this.filtersChange.emit({ ...this.currentFilters, minDiscount: value });
  }

  isAttrSelected(attrId: string, value: string): boolean {
    return (this.currentFilters.selectedAttributes[attrId] ?? []).includes(value);
  }

  onAttributeToggle(attrId: string, value: string): void {
    const current = this.currentFilters.selectedAttributes[attrId] ?? [];
    const updated = current.includes(value)
      ? current.filter((v) => v !== value)
      : [...current, value];
    const selectedAttributes = { ...this.currentFilters.selectedAttributes, [attrId]: updated };
    if (updated.length === 0) delete selectedAttributes[attrId];
    this.filtersChange.emit({ ...this.currentFilters, selectedAttributes });
  }

  clearAll(): void {
    this.filtersChange.emit({
      categoryId: null, minPrice: null, maxPrice: null, selectedBrandIds: [], minDiscount: null,
      selectedAttributes: {},
    });
  }
}
