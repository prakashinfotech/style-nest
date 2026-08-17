import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CartService } from './cart.service';
import type { CartItem } from '../models/cart.model';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  const BASE = 'http://localhost:5004/api/v1';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CartService, provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('addItem() should call POST /api/v1/cart/items with correct body', () => {
    const mockItem: CartItem = {
      id: 'item-1', productId: 'product-1', variantId: null,
      name: 'Test Product', imageUrl: '', price: 100, salePrice: null,
      quantity: 2, size: 'M', colour: null,
    };

    service.addItem('product-1', 'M', null, 2).subscribe((item) => {
      expect(item).toEqual(mockItem);
    });

    const req = httpMock.expectOne(`${BASE}/cart/items`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      productId: 'product-1', size: 'M', colour: null, quantity: 2,
    });
    req.flush(mockItem);
  });

  it('removeItem() should call DELETE /api/v1/cart/items/{id}', () => {
    service.removeItem('item-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/cart/items/item-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
