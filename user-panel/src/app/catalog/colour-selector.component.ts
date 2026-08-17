import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductVariant } from '../core/models/product.model';

export interface ColourOption {
  name: string;
  hex: string;
}

@Component({
  selector: 'app-colour-selector',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div>
      <p class="text-sm font-semibold text-dark mb-2">
        Colour: <span class="font-normal text-muted">{{ selectedColour ?? 'Select' }}</span>
      </p>
      <div class="flex flex-wrap gap-2" role="radiogroup" aria-label="Available colours">
        @for (colour of colours; track colour.name) {
          @let oos = isOutOfStock(colour.name);
          <div class="relative">
            <button
              class="w-8 h-8 rounded-full border-2 transition"
              [class.border-navy]="selectedColour === colour.name && !oos"
              [class.border-transparent]="selectedColour !== colour.name && !oos"
              [class.border-border]="oos"
              [class.opacity-40]="oos"
              [class.cursor-not-allowed]="oos"
              [style.background]="colour.hex"
              [disabled]="oos"
              role="radio"
              [attr.aria-checked]="selectedColour === colour.name"
              [attr.aria-label]="colour.name + (oos ? ' — out of stock' : '')"
              [title]="colour.name + (oos ? ' (out of stock)' : '')"
              (click)="!oos && colourChange.emit(colour.name)"
            ></button>
            <!-- OOS diagonal slash overlay -->
            @if (oos) {
              <span
                class="absolute inset-0 flex items-center justify-center pointer-events-none"
                aria-hidden="true"
              >
                <svg class="w-8 h-8 text-muted/60" viewBox="0 0 32 32">
                  <line x1="4" y1="4" x2="28" y2="28" stroke="currentColor" stroke-width="2"/>
                </svg>
              </span>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class ColourSelectorComponent {
  @Input({ required: true }) colours: ColourOption[] = [];
  @Input() selectedColour: string | null = null;
  /** Full variant list — used to grey-out colours with 0 stock for the selected size. */
  @Input() variants: ProductVariant[] = [];
  @Input() selectedSize: string | null = null;
  @Output() colourChange = new EventEmitter<string>();

  /**
   * A colour is OOS when every variant matching the current size (if any)
   * and this colour has stockQuantity === 0.
   */
  isOutOfStock(colourName: string): boolean {
    let matching = this.variants.filter((v) => v.colour === colourName);
    if (this.selectedSize) {
      matching = matching.filter((v) => v.size === this.selectedSize);
    }
    return matching.length > 0 && matching.every((v) => v.stockQuantity === 0);
  }
}
