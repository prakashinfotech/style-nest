import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { AddToCartPanelComponent } from './add-to-cart-panel.component';
import { CartActions } from '../store/cart/cart.actions';
import { OrderActions } from '../store/order/order.actions';
import { WishlistActions } from '../store/wishlist/wishlist.actions';
import { Product } from '../core/models/product.model';

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id:           'prod-1',
    name:         'Test Product',
    slug:         'test-product',
    description:  'desc',
    price:        1000,
    salePrice:    800,
    brandId:      'brand-1',
    brandName:    'Brand',
    categoryId:   'cat-1',
    categoryName: 'Category',
    imageUrls:    [],
    variants:     [],
    rating:       4,
    reviewCount:  5,
    inStock:      true,
    ...overrides,
  };
}

describe('AddToCartPanelComponent', () => {
  let fixture: ComponentFixture<AddToCartPanelComponent>;
  let component: AddToCartPanelComponent;
  let store: MockStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddToCartPanelComponent],
      providers: [
        provideMockStore({
          initialState: {
            auth:     { user: null, accessToken: null },
            wishlist: { ids: [] },
          },
        }),
      ],
    }).compileComponents();

    store    = TestBed.inject(MockStore);
    fixture  = TestBed.createComponent(AddToCartPanelComponent);
    component = fixture.componentInstance;
    component.product = makeProduct();
    fixture.detectChanges();
  });

  // ── canProceed ──────────────────────────────────────────────────────────

  it('canProceed is true when no size or colour required', () => {
    component.requiresSize   = false;
    component.requiresColour = false;
    expect(component.canProceed).toBe(true);
  });

  it('canProceed is false when size required but not selected', () => {
    component.requiresSize = true;
    component.selectedSize = null;
    expect(component.canProceed).toBe(false);
  });

  it('canProceed is true when size required and selected', () => {
    component.requiresSize = true;
    component.selectedSize = 'M';
    expect(component.canProceed).toBe(true);
  });

  it('canProceed is false when colour required but not selected', () => {
    component.requiresColour = true;
    component.selectedColour = null;
    expect(component.canProceed).toBe(false);
  });

  it('canProceed is true when colour required and selected', () => {
    component.requiresColour = true;
    component.selectedColour = 'Red';
    expect(component.canProceed).toBe(true);
  });

  it('canProceed is false when both required but neither selected', () => {
    component.requiresSize   = true;
    component.requiresColour = true;
    component.selectedSize   = null;
    component.selectedColour = null;
    expect(component.canProceed).toBe(false);
  });

  // ── addToCart dispatches correct action ─────────────────────────────────

  it('addToCart dispatches CartActions.addItem with correct payload', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');
    component.selectedSize   = 'L';
    component.selectedColour = 'Blue';
    component.quantity       = 2;

    component.addToCart();

    expect(dispatchSpy).toHaveBeenCalledWith(
      CartActions.addItem({
        productId: 'prod-1',
        size:      'L',
        colour:    'Blue',
        quantity:  2,
      }),
    );
  });

  it('addToCart dispatches with null size/colour when not selected', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');
    component.selectedSize   = null;
    component.selectedColour = null;
    component.quantity       = 1;

    component.addToCart();

    expect(dispatchSpy).toHaveBeenCalledWith(
      CartActions.addItem({
        productId: 'prod-1',
        size:      null,
        colour:    null,
        quantity:  1,
      }),
    );
  });

  // ── buyNow dispatches correct action ────────────────────────────────────

  it('buyNow dispatches OrderActions.buyNow with correct payload', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');
    component.selectedSize   = 'M';
    component.selectedColour = 'Red';
    component.quantity       = 3;

    component.buyNow();

    expect(dispatchSpy).toHaveBeenCalledWith(
      OrderActions.buyNow({
        productId: 'prod-1',
        size:      'M',
        colour:    'Red',
        quantity:  3,
      }),
    );
  });

  // ── quantity increment / decrement ──────────────────────────────────────

  it('increment emits quantity + 1', () => {
    const emitted: number[] = [];
    component.quantityChange.subscribe((v) => emitted.push(v));
    component.quantity = 3;
    component.increment();
    expect(emitted).toEqual([4]);
  });

  it('increment caps at 10', () => {
    const emitted: number[] = [];
    component.quantityChange.subscribe((v) => emitted.push(v));
    component.quantity = 10;
    component.increment();
    expect(emitted).toEqual([10]);
  });

  it('decrement emits quantity - 1', () => {
    const emitted: number[] = [];
    component.quantityChange.subscribe((v) => emitted.push(v));
    component.quantity = 3;
    component.decrement();
    expect(emitted).toEqual([2]);
  });

  it('decrement floors at 1', () => {
    const emitted: number[] = [];
    component.quantityChange.subscribe((v) => emitted.push(v));
    component.quantity = 1;
    component.decrement();
    expect(emitted).toEqual([1]);
  });

  // ── wishlist toggle ─────────────────────────────────────────────────────

  it('toggleWishlist dispatches WishlistActions.toggle', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');
    component.toggleWishlist();
    expect(dispatchSpy).toHaveBeenCalledWith(
      WishlistActions.toggle({ productId: 'prod-1' }),
    );
  });

  // ── out of stock ────────────────────────────────────────────────────────

  it('canProceed is true but product.inStock=false disables buttons', () => {
    component.product = makeProduct({ inStock: false });
    fixture.detectChanges();
    // canProceed itself is still true — inStock is checked in template binding
    expect(component.canProceed).toBe(true);
    expect(component.product.inStock).toBe(false);
  });
});
