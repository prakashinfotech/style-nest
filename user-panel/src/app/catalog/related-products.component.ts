import {
  ChangeDetectionStrategy,
  Component,
  Input,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { ProductCardComponent } from './product-card.component';
import { SectionHeaderComponent } from '../shared/components/section-header.component';
import { selectRelatedProducts } from '../store/catalog/catalog.selectors';
import { selectWishlistIds } from '../store/wishlist/wishlist.selectors';
import { Product } from '../core/models/product.model';

@Component({
  selector: 'app-related-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ProductCardComponent, SectionHeaderComponent],
  template: `
    @if (related().length > 0) {
      <section class="border-t border-gray-100 pt-6" aria-labelledby="related-heading">
        <app-section-header
          eyebrow="You May Also Like"
          title="Related Products"
        />

        <!-- Horizontal scroll on mobile, 3-col grid on desktop -->
        <div
          class="flex gap-4 overflow-x-auto pb-2 md:overflow-visible md:grid md:grid-cols-3 md:pb-0
                 scrollbar-hide snap-x snap-mandatory"
          role="list"
          aria-label="Related products"
        >
          @for (product of related(); track product.id) {
            <div
              class="flex-shrink-0 w-48 md:w-auto snap-start"
              role="listitem"
            >
              <app-product-card
                [product]="product"
                [isWishlisted]="wishlistIds().includes(product.id)"
              />
            </div>
          }
        </div>
      </section>
    }
  `,
})
export class RelatedProductsComponent {
  @Input() currentProductId = '';

  private readonly store = inject(Store);

  readonly related     = toSignal(this.store.select(selectRelatedProducts), { initialValue: [] as Product[] });
  readonly wishlistIds = toSignal(this.store.select(selectWishlistIds),     { initialValue: [] as string[] });
}
