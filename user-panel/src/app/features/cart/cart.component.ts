import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AsyncPipe, CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { CartActions } from '../../store/cart/cart.actions';
import {
  selectCart, selectCartCount, selectCartItems, selectCartLoading,
  selectCouponMessage, selectCouponStatus, selectSavedForLater,
} from '../../store/cart/cart.selectors';
import { CartItem } from '../../core/models/cart.model';
import { CurrencyInrPipe } from '../../shared/pipes/currency-inr.pipe';
import { CartItemComponent } from '../../cart/cart-item.component';
import { CartSummaryComponent } from '../../cart/cart-summary.component';
import { CouponInputComponent } from '../../cart/coupon-input.component';
import { SkeletonLoaderComponent } from '../../shared/components/skeleton-loader.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-cart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, AsyncPipe,
    CartItemComponent, CartSummaryComponent,
    CouponInputComponent, SkeletonLoaderComponent,
    EmptyStateComponent, CurrencyInrPipe,
  ],
  template: `
    <div class="max-w-layout mx-auto px-4 py-6 min-h-screen">
      <h1 class="text-xl md:text-2xl font-bold text-dark mb-6">
        My Bag
        @if ((cartCount$ | async) ?? 0; as count) {
          @if (count > 0) {
            <span class="text-base text-muted font-normal ml-2">({{ count }} items)</span>
          }
        }
      </h1>

      @if (isLoading$ | async) {
        <div class="flex flex-col lg:flex-row gap-6">
          <div class="flex-1 space-y-4">
            @for (n of [1, 2, 3]; track n) {
              <app-skeleton-loader height="120px" cssClass="rounded-lg" />
            }
          </div>
          <div class="lg:w-72">
            <app-skeleton-loader height="300px" cssClass="rounded-lg" />
          </div>
        </div>
      } @else if ((cartItems$ | async)?.length ?? 0; as itemCount) {
        @if (itemCount > 0) {
          <div class="flex flex-col lg:flex-row gap-6">
            <!-- Items list -->
            <div class="flex-1">
              <div class="bg-card rounded-lg border border-gray-100 px-4">
                @for (item of cartItems$ | async; track item.id) {
                  <app-cart-item
                    [item]="item"
                    (quantityChange)="updateQuantity(item.id, $event)"
                    (remove)="removeItem(item.id)"
                    (saveForLater)="saveForLater(item.id)"
                  />
                }
              </div>

              <div class="mt-4">
                <app-coupon-input
                  [couponStatus]="(couponStatus$ | async) ?? 'idle'"
                  [couponMessage]="couponMessage$ | async"
                  (applyCoupon)="applyCoupon($event)"
                />
              </div>
            </div>

            <!-- Summary -->
            <div class="lg:w-80">
              @if (cart$ | async; as cart) {
                <app-cart-summary [cart]="cart" [itemCount]="itemCount" />
              }
            </div>
          </div>

          <!-- Saved for later -->
          @if ((savedForLater$ | async)?.length ?? 0; as savedCount) {
            @if (savedCount > 0) {
              <div class="mt-8">
                <h2 class="text-base font-semibold text-dark mb-3">Saved for Later ({{ savedCount }})</h2>
                <div class="bg-card rounded-lg border border-border divide-y divide-border px-4">
                  @for (saved of savedForLater$ | async; track saved.id) {
                    <div class="py-3 flex items-center gap-4">
                      @if (saved.imageUrl) {
                        <img [src]="saved.imageUrl" [alt]="saved.name"
                             class="w-14 h-16 object-cover rounded-lg border border-border shrink-0" loading="lazy" />
                      }
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-medium text-dark line-clamp-1">{{ saved.name }}</p>
                        <p class="text-xs font-semibold text-dark mt-0.5">{{ (saved.salePrice ?? saved.price) | currencyInr }}</p>
                      </div>
                      <div class="flex flex-col items-end gap-1.5 shrink-0">
                        <button type="button" (click)="moveToCart(saved)"
                                class="text-xs text-red hover:underline font-medium">Move to bag</button>
                        <button type="button" (click)="removeSaved(saved.id)"
                                class="text-xs text-muted hover:text-dark">Remove</button>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }
          }
        } @else {
          <app-empty-state
            icon="🛍️"
            title="Your bag is empty"
            subtitle="Looks like you haven't added anything yet."
            ctaLabel="Start Shopping"
            ctaRoute="/products"
          />
        }
      } @else {
        <app-empty-state
          icon="🛍️"
          title="Your bag is empty"
          subtitle="Looks like you haven't added anything yet."
          ctaLabel="Start Shopping"
          ctaRoute="/products"
        />
      }
    </div>
  `,
})
export class CartComponent implements OnInit {
  private readonly store = inject(Store);

  readonly cart$           = this.store.select(selectCart);
  readonly cartItems$      = this.store.select(selectCartItems);
  readonly cartCount$      = this.store.select(selectCartCount);
  readonly isLoading$      = this.store.select(selectCartLoading);
  readonly couponStatus$   = this.store.select(selectCouponStatus);
  readonly couponMessage$  = this.store.select(selectCouponMessage);
  readonly savedForLater$  = this.store.select(selectSavedForLater);

  ngOnInit(): void {
    this.store.dispatch(CartActions.loadCart());
  }

  updateQuantity(itemId: string, quantity: number): void {
    this.store.dispatch(CartActions.updateItem({ itemId, quantity }));
  }

  removeItem(itemId: string): void {
    this.store.dispatch(CartActions.removeItem({ itemId }));
  }

  saveForLater(itemId: string): void {
    this.store.dispatch(CartActions.saveForLater({ itemId }));
  }

  moveToCart(item: CartItem): void {
    this.store.dispatch(CartActions.moveToCart({ item }));
  }

  removeSaved(itemId: string): void {
    this.store.dispatch(CartActions.removeSaved({ itemId }));
  }

  applyCoupon(couponCode: string): void {
    this.store.dispatch(CartActions.applyCoupon({ couponCode }));
  }
}
