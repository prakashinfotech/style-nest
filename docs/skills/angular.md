# Angular Component & Service Patterns
# Author: Lead Dev — complete before Phase 3 prompts

## Component Template

Every component must follow this pattern exactly:

```ts
import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, AsyncPipe],
  templateUrl: './example.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExampleComponent {
  // inject via inject() function, not constructor
}
```

Rules:
- `standalone: true` on every component — no NgModules
- `ChangeDetectionStrategy.OnPush` always
- Use `inject()` function for DI (Angular 14+ style)
- `AsyncPipe` only — NO `subscribe()` in component classes
- One component per file, one responsibility

## Service Template

```ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ExampleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/example';

  getAll(): Observable<ExampleDto[]> {
    return this.http.get<ExampleDto[]>(this.baseUrl);
  }
}
```

Rules:
- `HttpClient` lives ONLY in services — never in components
- Return `Observable<T>` — do not subscribe inside services

## NgRx Pattern

Actions:
```ts
export const loadProducts = createAction('[Catalog] Load Products', props<{ filters: FilterState }>());
export const loadProductsSuccess = createAction('[Catalog] Load Products Success', props<{ products: Product[] }>());
export const loadProductsFailure = createAction('[Catalog] Load Products Failure', props<{ error: string }>());
```

Effects:
```ts
loadProducts$ = createEffect(() =>
  this.actions$.pipe(
    ofType(loadProducts),
    switchMap(({ filters }) =>
      this.catalogService.getProducts(filters).pipe(
        map(products => loadProductsSuccess({ products })),
        catchError(err => of(loadProductsFailure({ error: err.message })))
      )
    )
  )
);
```

## Routing (lazy loading)
```ts
{
  path: 'search',
  loadComponent: () => import('./features/catalog/plp.component').then(m => m.PlpComponent)
}
```

## Template Best Practices
- `@if` / `@for` (Angular 17+ control flow) — not *ngIf / *ngFor
- `trackBy` on all `@for` loops
- Always provide `alt` text on `<img>`
- Tailwind responsive classes in every template: `class="... md:... lg:..."`
