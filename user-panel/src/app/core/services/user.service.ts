import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
}

export interface Address {
  id: string;
  label: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  state: string;
  pincode: string;
  isDefault: boolean;
}

export interface WalletBalance {
  balance: number;
  currency: string;
}

export interface WalletTransaction {
  id: string;
  amount: number;
  type: 'Credit' | 'Debit';
  description: string;
  createdAt: string;
}

export interface Notification {
  id: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.userApiUrl;

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.base}/users/me`);
  }

  updateProfile(request: UpdateProfileRequest): Observable<User> {
    return this.http.put<User>(`${this.base}/users/me`, request);
  }

  getAddresses(): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.base}/users/me/addresses`);
  }

  addAddress(address: Omit<Address, 'id' | 'isDefault'>): Observable<Address> {
    return this.http.post<Address>(`${this.base}/users/me/addresses`, address);
  }

  updateAddress(id: string, address: Omit<Address, 'id'>): Observable<Address> {
    return this.http.put<Address>(`${this.base}/users/me/addresses/${id}`, address);
  }

  deleteAddress(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/me/addresses/${id}`);
  }

  getWishlist(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/users/me/wishlist`);
  }

  addToWishlist(productId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/me/wishlist/${productId}`, {});
  }

  removeFromWishlist(productId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/me/wishlist/${productId}`);
  }

  getWallet(): Observable<WalletBalance> {
    return this.http.get<WalletBalance>(`${this.base}/users/me/wallet`);
  }

  addMoneyToWallet(amount: number): Observable<WalletBalance> {
    return this.http.post<WalletBalance>(`${this.base}/users/me/wallet/add-money`, { amount });
  }

  getWalletTransactions(): Observable<WalletTransaction[]> {
    return this.http.get<WalletTransaction[]>(`${this.base}/users/me/wallet/transactions`);
  }

  getNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.base}/users/me/notifications`);
  }

  getUnreadNotificationCount(): Observable<number> {
    return this.http.get<number>(`${this.base}/users/me/notifications/unread-count`);
  }

  markNotificationRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/me/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<void> {
    return this.http.post<void>(`${this.base}/users/me/notifications/read-all`, {});
  }
}
