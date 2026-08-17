import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart, CartItem } from '../models/cart.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.cartApiUrl;

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(`${this.base}/cart`);
  }

  addItem(productId: string, size: string | null, colour: string | null, quantity: number): Observable<CartItem> {
    return this.http.post<CartItem>(`${this.base}/cart/items`, { productId, size, colour, quantity });
  }

  updateItem(itemId: string, quantity: number): Observable<CartItem> {
    return this.http.put<CartItem>(`${this.base}/cart/items/${itemId}`, { quantity });
  }

  removeItem(itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/cart/items/${itemId}`);
  }

  applyCoupon(couponCode: string): Observable<Cart> {
    return this.http.post<Cart>(`${this.base}/cart/coupon`, { code: couponCode });
  }
}
