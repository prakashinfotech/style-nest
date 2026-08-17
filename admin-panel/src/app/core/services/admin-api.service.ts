import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DashboardMetrics {
  totalOrders: number;
  totalRevenue: number;
  totalUsers: number;
  totalProducts: number;
  totalSellers: number;
  pendingSellers: number;
  totalBrands: number;
  totalCategories: number;
}

export interface RevenueData {
  date: string;
  revenue: number;
  orderCount: number;
}

export interface AdminOrder {
  id: string;
  orderNumber: string;
  userEmail: string;
  totalAmount: number;
  status: string;
  createdAt: string;
  itemCount: number;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  emailConfirmed: boolean;
  createdAt: string;
}

export interface AdminProduct {
  id: string;
  name: string;
  brandName: string;
  categoryName: string;
  price: number;
  inStock: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface SellerSummary {
  id: string;
  storeName: string;
  email: string;
  status: string;
  commissionRate: number;
  productCount: number;
  createdAt: string;
}

export interface AdminStaff {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditLog {
  id: string;
  actorEmail: string;
  actorRole: string;
  action: string;
  resource: string;
  resourceId: string | null;
  ipAddress: string;
  timestamp: string;
  details: string | null;
}

export interface ReviewModerationItem {
  id: string;
  productName: string;
  authorEmail: string;
  rating: number;
  title: string;
  body: string;
  status: string;
  createdAt: string;
}

/** ENH-ADMIN-006 — Search synonym entry returned by Admin API. */
export interface SearchSynonymDto {
  id: string;
  term: string;
  synonyms: string[];
  updatedAt: string;
}

/** ENH-ADMIN-006 — Upsert payload for a synonym entry. */
export interface UpsertSynonymRequest {
  term: string;
  synonyms: string[];
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/admin';

  // Dashboard
  getMetrics(): Observable<DashboardMetrics> {
    return this.http.get<DashboardMetrics>(`${this.base}/dashboard/metrics`);
  }

  getRevenue(days = 30): Observable<RevenueData[]> {
    return this.http.get<RevenueData[]>(`${this.base}/dashboard/revenue`, {
      params: new HttpParams().set('days', days),
    });
  }

  // Orders
  getOrders(page = 1, pageSize = 20): Observable<PagedResult<AdminOrder>> {
    return this.http.get<PagedResult<AdminOrder>>(`${this.base}/orders`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  updateOrderStatus(id: string, status: string): Observable<AdminOrder> {
    return this.http.put<AdminOrder>(`${this.base}/orders/${id}/status`, { status });
  }

  // Products
  getProducts(page = 1, pageSize = 20): Observable<PagedResult<AdminProduct>> {
    return this.http.get<PagedResult<AdminProduct>>(`${this.base}/products`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  updateProductStatus(id: string, isActive: boolean): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.base}/products/${id}/status`, { isActive });
  }

  // Users
  getUsers(page = 1, pageSize = 20): Observable<PagedResult<AdminUser>> {
    return this.http.get<PagedResult<AdminUser>>(`${this.base}/users`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  // Super Admin — Sellers
  getSellers(status?: string): Observable<SellerSummary[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<SellerSummary[]>(`${this.base}/superadmin/sellers`, { params });
  }

  approveSeller(id: string, approve: boolean, rejectionReason?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/superadmin/sellers/${id}/approve`, {
      approve,
      rejectionReason,
    });
  }

  // Super Admin — Admins
  getAdminStaff(): Observable<AdminStaff[]> {
    return this.http.get<AdminStaff[]>(`${this.base}/superadmin/admins`);
  }

  createAdmin(data: { firstName: string; lastName: string; email: string; password: string }): Observable<void> {
    return this.http.post<void>(`${this.base}/superadmin/admins`, data);
  }

  suspendUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/superadmin/users/${id}/suspend`, {});
  }

  // Audit Logs
  getAuditLogs(page = 1, pageSize = 20, action?: string, actorEmail?: string): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (action) params = params.set('action', action);
    if (actorEmail) params = params.set('actorEmail', actorEmail);
    return this.http.get<PagedResult<AuditLog>>(`${this.base}/superadmin/audit-logs`, { params });
  }

  // Review moderation
  getReviewsForModeration(page = 1, pageSize = 20, status?: string): Observable<PagedResult<ReviewModerationItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<ReviewModerationItem>>(`${this.base}/reviews`, { params });
  }

  approveReview(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/reviews/${id}/approve`, {});
  }

  deleteReview(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/reviews/${id}`);
  }

  // ENH-ADMIN-006 — Search Synonyms
  getSynonyms(): Observable<SearchSynonymDto[]> {
    return this.http.get<SearchSynonymDto[]>(`${this.base}/search/synonyms`);
  }

  upsertSynonym(request: UpsertSynonymRequest): Observable<SearchSynonymDto> {
    return this.http.put<SearchSynonymDto>(`${this.base}/search/synonyms`, request);
  }

  deleteSynonym(term: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/search/synonyms`, {
      params: new HttpParams().set('term', term),
    });
  }
}
