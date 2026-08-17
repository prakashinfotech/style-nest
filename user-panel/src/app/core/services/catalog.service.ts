import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category, PaginatedResult, Product, ProductFilters } from '../models/product.model';
import { CreateReviewRequest, PagedReviews, Review } from '../models/review.model';

/** Shape returned by the backend review endpoint */
interface BackendReview {
  id: string;
  productId: string;
  userId: string;
  author: string;
  rating: number;
  title: string;
  body: string;
  createdAt: string;
}

interface BackendPagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.catalogApiUrl;

  getProducts(filters: ProductFilters): Observable<PaginatedResult<Product>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.categoryId) params = params.set('categoryId', filters.categoryId);
    if (filters.brandId)    params = params.set('brandId', filters.brandId);
    if (filters.search)     params = params.set('search', filters.search);
    if (filters.minPrice != null)    params = params.set('minPrice', filters.minPrice);
    if (filters.maxPrice != null)    params = params.set('maxPrice', filters.maxPrice);
    if (filters.minDiscount != null) params = params.set('minDiscount', filters.minDiscount);
    if (filters.sort)                params = params.set('sort', filters.sort);

    return this.http.get<PaginatedResult<Product>>(`${this.base}/products`, { params });
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/products/${id}`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }

  getReviews(productId: string, page = 1, pageSize = 10): Observable<PagedReviews> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http
      .get<BackendPagedResult<BackendReview>>(
        `${this.base}/products/${productId}/reviews`,
        { params },
      )
      .pipe(
        map((res) => ({
          items: res.items.map(mapReview),
          totalCount: res.totalCount,
          page: res.page,
          pageSize: res.pageSize,
        })),
      );
  }

  postReview(productId: string, req: CreateReviewRequest): Observable<Review> {
    return this.http
      .post<BackendReview>(`${this.base}/products/${productId}/reviews`, req)
      .pipe(map(mapReview));
  }

  getRelatedProducts(productId: string, limit = 6): Observable<Product[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<Product[]>(`${this.base}/products/${productId}/related`, { params });
  }

  getCategoryAttributes(categoryId: string): Observable<AttributeDefinition[]> {
    return this.http.get<AttributeDefinition[]>(`${this.base}/catalog/categories/${categoryId}/attributes`);
  }

  // ── ENH-CAT-006 — Azure Cognitive Search ───────────────────────────────────

  /**
   * ENH-CAT-006 — Full-text search via Azure Cognitive Search with facets and highlights.
   * Falls back to DB-backed search on the backend when ACS is not configured.
   *
   * @param request.q         Free-text query
   * @param request.facets    Array of "fieldName:value1,value2" strings (AND-across/OR-within)
   * @param request.minPrice  Lower price bound
   * @param request.maxPrice  Upper price bound
   * @param request.sort      "relevance" | "price_asc" | "price_desc" | "name_asc"
   * @param request.page      1-based page number
   * @param request.pageSize  Items per page (max 100)
   */
  cognitiveSearch(request: CognitiveSearchRequest): Observable<CognitiveSearchResponse> {
    let params = new HttpParams()
      .set('page', request.page ?? 1)
      .set('pageSize', request.pageSize ?? 20);

    if (request.q)        params = params.set('q', request.q);
    if (request.minPrice != null) params = params.set('minPrice', request.minPrice);
    if (request.maxPrice != null) params = params.set('maxPrice', request.maxPrice);
    if (request.sort)     params = params.set('sort', request.sort);

    for (const facet of request.facets ?? []) {
      params = params.append('facets', facet);
    }

    return this.http.get<CognitiveSearchResponse>(`${this.base}/search`, { params });
  }

  /**
   * ENH-CAT-006 / ENH-SRCH-002 — Autocomplete suggestions.
   * Routes through ACS Suggester when available; falls back to DB prefix search.
   */
  getSearchSuggestions(q: string): Observable<string[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<{ query: string; suggestions: string[] }>(
      `${this.base}/search/suggest`, { params }
    ).pipe(map(r => r.suggestions));
  }
}

export interface AttributeDefinition {
  id: string;
  name: string;
  dataType: string;
  allowedValues: string[];
}

// ── ENH-CAT-006 — Azure Cognitive Search DTOs ─────────────────────────────────

export interface CognitiveSearchRequest {
  /** Full-text query */
  q?: string;
  /** Facet filters: "fieldName:value1,value2" format */
  facets?: string[];
  minPrice?: number;
  maxPrice?: number;
  /** "relevance" | "price_asc" | "price_desc" | "name_asc" */
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface FacetValueDto {
  value: string;
  count: number;
}

export interface FacetResultDto {
  name: string;
  values: FacetValueDto[];
}

export interface SearchHitDto {
  id: string;
  name: string;
  description?: string;
  brand?: string;
  category?: string;
  price: number;
  imageUrl?: string;
  slug?: string;
  highlights?: Record<string, string[]>;
}

export interface CognitiveSearchResponse {
  query: string;
  totalCount: number;
  page: number;
  pageSize: number;
  results: SearchHitDto[];
  facets: FacetResultDto[];
}

function mapReview(r: BackendReview): Review {
  return {
    id:        r.id,
    productId: r.productId,
    userId:    r.userId,
    author:    r.author,
    rating:    r.rating,
    title:     r.title,
    body:      r.body,
    date:      r.createdAt,
  };
}
