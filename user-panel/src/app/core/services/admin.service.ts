import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type BannerPlacement = 'HeroCarousel' | 'CategoryStrip' | 'PromoBanner' | 'FlashSale';
export type DiscountType    = 'Percentage' | 'FlatAmount';

export interface Banner {
  id: string;
  title: string;
  imageUrl: string;
  linkUrl: string | null;
  placement: BannerPlacement;
  displayOrder: number;
  isActive: boolean;
  startsAt: string | null;
  endsAt: string | null;
}

export interface CreateBannerRequest {
  title: string;
  imageUrl: string;
  linkUrl: string | null;
  placement: BannerPlacement;
  displayOrder: number;
  isActive: boolean;
  startsAt: string | null;
  endsAt: string | null;
}

export interface Coupon {
  id: string;
  code: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  minOrderAmount: number | null;
  maxDiscountCap: number | null;
  usageLimitPerUser: number | null;
  totalUsageLimit: number | null;
  usedCount: number;
  isActive: boolean;
  startsAt: string | null;
  expiresAt: string | null;
}

export interface CreateCouponRequest {
  code: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  minOrderAmount: number | null;
  maxDiscountCap: number | null;
  usageLimitPerUser: number | null;
  totalUsageLimit: number | null;
  isActive: boolean;
  startsAt: string | null;
  expiresAt: string | null;
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

export interface CreateSellerRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface CreateSellerResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface DashboardMetrics {
  totalOrders: number;
  totalRevenue: number;
  totalUsers: number;
  totalProducts: number;
}

// ENH-AI-003 — Azure OpenAI Product Description Assistant
export interface GenerateDescriptionRequest {
  productName: string;
  category: string;
  brand: string;
  price: number;
  existingDescription?: string | null;
  keywords?: string[] | null;
}

export interface GenerateDescriptionResponse {
  description: string;
  disclaimer: string | null;
}

// ENH-ADMIN-002 — Job Management UI types
export interface AdminJob {
  /** Kebab-case identifier used in POST …/run calls */
  slug: string;
  /** Human-readable display name */
  name: string;
}

export interface JobRunResult {
  jobName: string;
  executedAt: string;
  success: boolean;
  message: string;
  [key: string]: unknown; // extra fields returned by specific job implementations
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.adminApiUrl;

  getBanners(): Observable<Banner[]> {
    return this.http.get<Banner[]>(`${this.base}/admin/banners`);
  }

  createBanner(req: CreateBannerRequest): Observable<Banner> {
    return this.http.post<Banner>(`${this.base}/admin/banners`, req);
  }

  deleteBanner(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/banners/${id}`);
  }

  getCoupons(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.base}/admin/coupons`);
  }

  createCoupon(req: CreateCouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(`${this.base}/admin/coupons`, req);
  }

  deleteCoupon(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/coupons/${id}`);
  }

  getAdminOrders(): Observable<AdminOrder[]> {
    return this.http.get<AdminOrder[]>(`${this.base}/admin/orders`);
  }

  getAdminUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.base}/admin/users`);
  }

  getAdminProducts(): Observable<AdminProduct[]> {
    return this.http.get<AdminProduct[]>(`${this.base}/admin/products`);
  }

  createSellerAccount(req: CreateSellerRequest): Observable<CreateSellerResponse> {
    return this.http.post<CreateSellerResponse>(`${this.base}/admin/users/create-seller`, req);
  }

  getDashboardMetrics(): Observable<DashboardMetrics> {
    return this.http.get<DashboardMetrics>(`${this.base}/admin/dashboard/metrics`);
  }

  // ENH-AI-003 — Azure OpenAI Product Description Assistant

  /** Calls the admin API to generate a product description via Azure OpenAI. */
  generateProductDescription(req: GenerateDescriptionRequest): Observable<GenerateDescriptionResponse> {
    return this.http.post<GenerateDescriptionResponse>(
      `${this.base}/admin/products/generate-description`, req);
  }

  // ENH-ADMIN-002 — Job Management UI

  /** Returns the list of registered scheduled jobs (slug + display name). */
  getAdminJobs(): Observable<AdminJob[]> {
    return this.http.get<AdminJob[]>(`${this.base}/admin/jobs`);
  }

  /** Immediately executes the specified job and returns the execution result. */
  runAdminJob(slug: string): Observable<JobRunResult> {
    return this.http.post<JobRunResult>(`${this.base}/admin/jobs/${slug}/run`, {});
  }
}
