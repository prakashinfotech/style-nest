import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PlaceOrderRequest {
  addressLine1:  string;
  addressLine2:  string;
  city:          string;
  state:         string;
  pincode:       string;
  paymentMethod: string;
  couponCode:    string | null;
}

export interface Order {
  id: string;
  orderNumber: string;
  status: string;
  total: number;
  createdAt: string;
  items: Array<{
    productId: string;
    name: string;
    imageUrl: string;
    price: number;
    quantity: number;
  }>;
}

export interface BuyNowRequest {
  productVariantId: string;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.orderApiUrl;

  placeOrder(request: PlaceOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.base}/orders`, request);
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/orders`);
  }

  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.base}/orders/${id}`);
  }

  cancelOrder(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/orders/${id}/cancel`, {});
  }

  buyNow(productId: string, size: string | null, colour: string | null, quantity: number): Observable<Order> {
    return this.http.post<Order>(`${this.base}/orders/buy-now`, { productId, size, colour, quantity });
  }

  requestReturn(orderId: string, reason: string, itemIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.base}/orders/${orderId}/return`, { reason, itemIds });
  }
}
