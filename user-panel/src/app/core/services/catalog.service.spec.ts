import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CatalogService } from './catalog.service';
import type { PaginatedResult, Product, Category } from '../models/product.model';

describe('CatalogService', () => {
  let service: CatalogService;
  let httpMock: HttpTestingController;

  const BASE = 'http://localhost:5005/api/v1';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CatalogService, provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(CatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getProducts() should call GET /api/v1/products with page and pageSize params', () => {
    const mockResult: PaginatedResult<Product> = {
      items: [], totalCount: 0, page: 1, pageSize: 24,
    };

    service.getProducts({
      categoryId: null, brandId: null, search: null,
      minPrice: null, maxPrice: null, minDiscount: null,
      sort: null, page: 1, pageSize: 24,
    }).subscribe((result) => {
      expect(result.totalCount).toBe(0);
      expect(result.items).toEqual([]);
    });

    const req = httpMock.expectOne((r) => r.url === `${BASE}/products`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('24');
    req.flush(mockResult);
  });

  it('getProduct() should call GET /api/v1/products/{id}', () => {
    const mockProduct = { id: 'prod-1', name: 'Test Product' } as Product;

    service.getProduct('prod-1').subscribe((product) => {
      expect(product.id).toBe('prod-1');
    });

    const req = httpMock.expectOne(`${BASE}/products/prod-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockProduct);
  });

  it('getCategories() should call GET /api/v1/categories', () => {
    const mockCategories: Category[] = [
      { id: 'cat-1', name: 'Women', slug: 'women', parentId: null, imageUrl: null },
    ];

    service.getCategories().subscribe((cats) => {
      expect(cats).toHaveLength(1);
      expect(cats[0].name).toBe('Women');
    });

    const req = httpMock.expectOne(`${BASE}/categories`);
    expect(req.request.method).toBe('GET');
    req.flush(mockCategories);
  });
});
