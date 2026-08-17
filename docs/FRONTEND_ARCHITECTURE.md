# FRONTEND_ARCHITECTURE.md — Angular Projects Architecture
> Internal architecture for both Angular 21 frontends.
> Both use standalone components, NgRx, and Tailwind CSS.

---

## 1. Project Overview

| Project | Port | Serves | Angular Config |
|---|---|---|---|
| `user-storefront/` | 4200 | Customers (guests + logged-in) | `angular.json` → `user-storefront` |
| `admin-panel/` | 4201 | Super Admin, Admin, Seller | `angular.json` → `admin-panel` |
| `shared-types/` | — | TypeScript interfaces shared across both | Local import (no npm publish) |

Both projects share:
- Tailwind config (via symlink or copy of `tailwind.config.js`)
- Design tokens (CSS variables)
- TypeScript interfaces from `shared-types/`

---

## 2. Angular Code Rules (Both Projects)

```typescript
// MANDATORY on every component
@Component({
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})

// Inject via inject() — not constructor
private readonly store = inject(Store);
private readonly router = inject(Router);

// Local UI state → Signals (not BehaviorSubject)
isLoading = signal(false);
isOpen = signal(false);

// Derived state → computed()
hasItems = computed(() => this.items().length > 0);

// Store observables → toSignal() in component
products = toSignal(this.store.select(selectProducts), { initialValue: [] });

// Templates → AsyncPipe for observables OR signals
// NEVER .subscribe() in component class

// @for loops → always trackBy
@for (item of items; track item.id) { ... }

// No 'any' anywhere — TypeScript strict mode
// No HttpClient calls inside components — Services only
// No logic in templates beyond simple boolean conditions
```

---

## 3. NgRx Patterns

### Action Naming

```typescript
// Pattern: [Feature] Verb Noun
export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login': props<{ email: string; password: string }>(),
    'Login Success': props<{ user: User; accessToken: string }>(),
    'Login Failure': props<{ error: string }>(),
    'Logout': emptyProps(),
  }
});
```

### Effect Pattern

```typescript
// No subscribe() — only RxJS operators
login$ = createEffect(() =>
  this.actions$.pipe(
    ofType(AuthActions.login),
    switchMap(({ email, password }) =>
      this.authService.login(email, password).pipe(
        map(response => AuthActions.loginSuccess({
          user: response.user,
          accessToken: response.accessToken
        })),
        catchError(error => of(AuthActions.loginFailure({
          error: error.message
        })))
      )
    )
  )
);
```

### Selector Pattern

```typescript
// Atomic selectors
export const selectCartItems = createSelector(
  selectCartState,
  (state) => state.items
);

// Derived selectors (computed from primitives)
export const selectCartTotal = createSelector(
  selectCartItems,
  selectAppliedCoupon,
  (items, coupon) => {
    const subtotal = items.reduce((sum, item) => sum + item.totalPrice, 0);
    const discount = coupon ? calculateDiscount(coupon, subtotal) : 0;
    return subtotal - discount;
  }
);
```

---

## 4. HTTP Interceptors

### Auth Interceptor

```typescript
// auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(Store);
  const token = store.selectSignal(selectAccessToken)();

  if (!token) return next(req);

  return next(req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }));
};
```

### Error Interceptor (Auto-Refresh)

```typescript
// error.interceptor.ts
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const store = inject(Store);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/refresh')) {
        return authService.refreshToken().pipe(
          switchMap(newToken => next(req.clone({
            setHeaders: { Authorization: `Bearer ${newToken}` }
          }))),
          catchError(() => {
            store.dispatch(AuthActions.logout());
            return throwError(() => error);
          })
        );
      }

      // Show toast for 4xx/5xx errors
      if (error.status >= 400) {
        store.dispatch(UiActions.showToast({
          message: error.error?.detail ?? 'Something went wrong',
          type: 'error'
        }));
      }

      return throwError(() => error);
    })
  );
};
```

### Loading Interceptor

```typescript
// loading.interceptor.ts — Global loading bar
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(Store);
  store.dispatch(UiActions.setGlobalLoading({ isLoading: true }));
  return next(req).pipe(
    finalize(() => store.dispatch(UiActions.setGlobalLoading({ isLoading: false })))
  );
};
```

---

## 5. Route Resolvers

```typescript
// pdp.resolver.ts — Pre-load product before route activates
export const pdpResolver: ResolveFn<void> = (route) => {
  const store = inject(Store);
  const productId = route.paramMap.get('id')!;

  store.dispatch(CatalogActions.loadProduct({ id: productId }));

  return store.select(selectSelectedProduct).pipe(
    filter(product => product !== null),
    take(1),
    map(() => void 0)
  );
};

// app.routes.ts
{
  path: 'products/:id',
  resolve: { product: pdpResolver },
  loadComponent: () => import('./features/catalog/pdp/pdp.component')
}
```

---

## 6. Shared Components Reference

### User Storefront — Shared Components

| Component | File | Purpose |
|---|---|---|
| `ProductCardComponent` | `shared/components/product-card/` | Product tile used in PLP, home, wishlist |
| `EmptyStateComponent` | `shared/components/empty-state/` | No data state (cart empty, no results) |
| `SkeletonLoaderComponent` | `shared/components/skeleton-loader/` | Loading placeholder |
| `ToastComponent` | `shared/components/toast/` | Global notification toasts |
| `BreadcrumbComponent` | `shared/components/breadcrumb/` | Navigation breadcrumbs |
| `SectionHeaderComponent` | `shared/components/section-header/` | Eyebrow + title + view-all |
| `StarRatingComponent` | `shared/components/star-rating/` | Star display (read-only + interactive) |
| `BackToTopComponent` | `shared/components/back-to-top/` | Fixed scroll-to-top button |
| `StickyProductBarComponent` | `shared/components/sticky-product-bar/` | PDP scroll-aware ATC bar |
| `SizeGuideModalComponent` | `shared/components/size-guide-modal/` | CDK overlay measurement table |

### Admin Panel — Shared Components

| Component | File | Purpose |
|---|---|---|
| `DataTableComponent` | `shared/components/data-table/` | Sortable, paginated table |
| `ConfirmDialogComponent` | `shared/components/confirm-dialog/` | CDK Dialog confirmation modal |
| `FileUploadComponent` | `shared/components/file-upload/` | Drag-drop + preview image uploader |
| `ChartCardComponent` | `shared/components/chart-card/` | ApexCharts wrapper with title |
| `StatusBadgeComponent` | `shared/components/status-badge/` | Color-coded order/seller status badge |
| `DynamicAttributeFormComponent` | `shared/components/dynamic-attribute-form/` | Category-driven product attribute fields |

---

## 7. Dynamic Attribute Form (Seller Product Creation)

This is the most complex component in the admin panel.

```typescript
// dynamic-attribute-form.component.ts
@Component({
  selector: 'app-dynamic-attribute-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (attr of attributes(); track attr.id) {
      <div class="attribute-field">
        <label [for]="attr.name">{{ attr.name }}
          @if (attr.isRequired) { <span class="text-red">*</span> }
        </label>

        @switch (attr.inputType) {
          @case ('text') {
            <input [formControlName]="attr.name" [id]="attr.name" />
          }
          @case ('select') {
            <mat-select [formControlName]="attr.name">
              @for (opt of attr.options; track opt) {
                <mat-option [value]="opt">{{ opt }}</mat-option>
              }
            </mat-select>
          }
          @case ('multi-select') {
            <mat-select [formControlName]="attr.name" multiple>
              @for (opt of attr.options; track opt) {
                <mat-option [value]="opt">{{ opt }}</mat-option>
              }
            </mat-select>
          }
          @case ('color-picker') {
            <app-color-picker [formControlName]="attr.name" />
          }
          @case ('number') {
            <input type="number" [formControlName]="attr.name" [id]="attr.name" />
          }
        }
      </div>
    }
  `
})
export class DynamicAttributeFormComponent implements OnInit {
  @Input({ required: true }) categoryId!: string;
  @Input({ required: true }) parentForm!: FormGroup;

  attributes = signal<AttributeDefinition[]>([]);

  private readonly catalogService = inject(CatalogService);

  ngOnInit(): void {
    this.catalogService.getCategoryAttributes(this.categoryId).pipe(
      takeUntilDestroyed()
    ).subscribe(attrs => {
      this.attributes.set(attrs);
      this.buildFormControls(attrs);
    });
  }

  private buildFormControls(attrs: AttributeDefinition[]): void {
    for (const attr of attrs) {
      const validators = attr.isRequired ? [Validators.required] : [];
      const control = attr.inputType === 'multi-select'
        ? new FormControl([], validators)
        : new FormControl('', validators);
      this.parentForm.addControl(`attr_${attr.id}`, control);
    }
  }
}
```

---

## 8. Lazy Loading Strategy

Every feature is lazy-loaded. No eager imports in `app.routes.ts` except layout components.

```typescript
// user-storefront/app.routes.ts
export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home.component')
          .then(m => m.HomeComponent)
      },
      {
        path: 'products/:id',
        resolve: { product: pdpResolver },
        loadComponent: () => import('./features/catalog/pdp/pdp.component')
          .then(m => m.PdpComponent)
      },
      {
        path: 'cart',
        loadComponent: () => import('./features/cart/cart.component')
          .then(m => m.CartComponent)
      },
      {
        path: 'checkout',
        canActivate: [authGuard],
        loadComponent: () => import('./features/checkout/checkout.component')
          .then(m => m.CheckoutComponent)
      },
      // ... all routes lazy
    ]
  }
];
```

---

## 9. Pipe Reference

| Pipe | File | Usage |
|---|---|---|
| `CurrencyInrPipe` | `shared/pipes/currency-inr.pipe.ts` | `{{ price \| currencyInr }}` → `₹1,299` |
| `TruncatePipe` | `shared/pipes/truncate.pipe.ts` | `{{ desc \| truncate:100 }}` |
| `TimeAgoPipe` | `shared/pipes/time-ago.pipe.ts` | `{{ date \| timeAgo }}` → `2 days ago` |

---

## 10. Angular Material Theme (Admin Panel)

The admin panel uses a custom Material Design 3 theme suited for dashboard UIs:

```scss
// admin-panel/src/styles.scss
@use '@angular/material' as mat;

$admin-theme: mat.define-theme((
  color: (
    theme-type: light,
    primary: mat.$blue-palette,
  ),
  typography: (
    brand-family: 'DM Sans, sans-serif',
    bold-weight: 600,
  ),
));

:root {
  @include mat.all-component-themes($admin-theme);
}
```

---

## 11. Environment Configuration

### User Storefront

```typescript
// user-storefront/src/environments/environment.ts (dev)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api/v1',   // Via YARP gateway
  signalRUrl: 'http://localhost:5005/hubs',
};

// user-storefront/src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com/api/v1',
  signalRUrl: 'https://api.yourdomain.com/hubs',
};
```

### Admin Panel

```typescript
// admin-panel/src/environments/environment.ts (dev)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api/v1',   // Same gateway, different role in JWT
  signalRUrl: 'http://localhost:5005/hubs',
};
```

---

## 12. Proxy Configuration (Dev Only)

```json
// user-storefront/proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  },
  "/hubs": {
    "target": "http://localhost:5005",
    "secure": false,
    "ws": true
  }
}
```

---

## 13. Testing Standards

### Unit Tests (Jasmine + Karma)

```typescript
// Every component with a spec file
describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;
  let store: MockStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCardComponent],
      providers: [provideMockStore({ initialState })]
    }).compileComponents();

    store = TestBed.inject<MockStore>(MockStore);
    fixture = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;
  });

  it('should dispatch wishlist toggle on heart click', () => {
    const dispatchSpy = spyOn(store, 'dispatch');
    component.product = mockProduct;
    fixture.detectChanges();

    const heartBtn = fixture.nativeElement.querySelector('[data-testid="wishlist-btn"]');
    heartBtn.click();

    expect(dispatchSpy).toHaveBeenCalledWith(
      WishlistActions.toggle({ productId: mockProduct.id })
    );
  });
});
```

### E2E Tests (Playwright)

```typescript
// e2e/checkout.spec.ts
test('complete checkout flow', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="product-card"]:first-child');
  await page.click('[data-testid="select-size-M"]');
  await page.click('[data-testid="add-to-cart"]');
  await page.goto('/cart');
  await page.click('[data-testid="proceed-to-checkout"]');
  // ... assert order confirmation
});
```

---

*See [ARCHITECTURE.md](ARCHITECTURE.md) for system-level decisions.*
*See [DESIGN.md](DESIGN.md) for component visual specifications.*
