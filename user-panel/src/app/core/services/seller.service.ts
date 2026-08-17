import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SellerProduct {
  id: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  mrp: number | null;
  brandId: string;
  brandName: string;
  categoryId: string;
  categoryName: string;
  imageUrls: string[];
  inStock: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreateSellerProductRequest {
  name: string;
  description: string;
  brandId: string;
  categoryId: string;
  price: number;
  mrp: number | null;
  imageUrls: string[];
}

export interface UpdateSellerProductRequest {
  name: string;
  description: string;
  price: number;
  mrp: number | null;
  isActive: boolean;
}

export interface SellerOrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface SellerOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  items: SellerOrderItem[];
}

export interface SellerCategory {
  id: string;
  name: string;
  slug: string;
}

export interface SellerBrand {
  id: string;
  name: string;
  slug: string;
}

@Injectable({ providedIn: 'root' })
export class SellerService {
  private readonly http        = inject(HttpClient);
  private readonly catalogBase = environment.catalogApiUrl;
  private readonly orderBase   = environment.orderApiUrl;

  getMyProducts(): Observable<SellerProduct[]> {
    return this.http.get<SellerProduct[]>(`${this.catalogBase}/seller/products`);
  }

  createProduct(req: CreateSellerProductRequest): Observable<SellerProduct> {
    return this.http.post<SellerProduct>(`${this.catalogBase}/seller/products`, req);
  }

  updateProduct(id: string, req: UpdateSellerProductRequest): Observable<SellerProduct> {
    return this.http.put<SellerProduct>(`${this.catalogBase}/seller/products/${id}`, req);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.catalogBase}/seller/products/${id}`);
  }

  getMyOrders(): Observable<SellerOrder[]> {
    return this.http.get<SellerOrder[]>(`${this.orderBase}/seller/orders`);
  }

  getCategories(): Observable<SellerCategory[]> {
    return this.http.get<SellerCategory[]>(`${this.catalogBase}/categories`);
  }

  getBrands(): Observable<SellerBrand[]> {
    return this.http.get<SellerBrand[]>(`${this.catalogBase}/brands`);
  }
}
