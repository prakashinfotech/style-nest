import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { CreateSellerProductRequest, SellerService } from '../../core/services/seller.service';

@Component({
  selector: 'app-seller-product-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, AsyncPipe, RouterLink],
  template: `
    <div class="p-4 md:p-8">
      <div class="max-w-2xl">
        <a routerLink="/seller/products"
           class="text-sm text-blue hover:underline inline-flex items-center gap-1 mb-4">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
          </svg>
          My Products
        </a>
        <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">Add New Product</h1>

        @if (submitError()) {
          <div class="mb-4 p-3 bg-red/10 border border-red/20 rounded-lg text-sm text-red">
            {{ submitError() }}
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-5">

          <!-- Name -->
          <div>
            <label class="block text-sm font-medium text-dark mb-1.5">
              Product Name <span class="text-red">*</span>
            </label>
            <input formControlName="name" type="text" placeholder="e.g. Women's Floral Kurta"
                   class="w-full px-3 py-2.5 rounded-lg border bg-card text-dark text-sm
                          placeholder:text-muted focus:outline-none focus:ring-2
                          focus:ring-navy/30 focus:border-navy transition-colors"
                   [class.border-red]="name.invalid && name.touched"
                   [class.border-border]="!(name.invalid && name.touched)">
            @if (name.invalid && name.touched) {
              <p class="text-xs text-red mt-1">Product name is required (max 200 chars).</p>
            }
          </div>

          <!-- Description -->
          <div>
            <label class="block text-sm font-medium text-dark mb-1.5">
              Description <span class="text-red">*</span>
            </label>
            <textarea formControlName="description" rows="4"
                      placeholder="Describe your product..."
                      class="w-full px-3 py-2.5 rounded-lg border bg-card text-dark text-sm
                             placeholder:text-muted focus:outline-none focus:ring-2
                             focus:ring-navy/30 focus:border-navy transition-colors resize-none"
                      [class.border-red]="description.invalid && description.touched"
                      [class.border-border]="!(description.invalid && description.touched)">
            </textarea>
            @if (description.invalid && description.touched) {
              <p class="text-xs text-red mt-1">Description is required.</p>
            }
          </div>

          <!-- Category + Brand -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-dark mb-1.5">
                Category <span class="text-red">*</span>
              </label>
              <select formControlName="categoryId"
                      class="w-full px-3 py-2.5 rounded-lg border border-border bg-card
                             text-dark text-sm focus:outline-none focus:ring-2
                             focus:ring-navy/30 focus:border-navy">
                <option value="">Select category</option>
                @if (categories$ | async; as cats) {
                  @for (cat of cats; track cat.id) {
                    <option [value]="cat.id">{{ cat.name }}</option>
                  }
                }
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-dark mb-1.5">
                Brand <span class="text-red">*</span>
              </label>
              <select formControlName="brandId"
                      class="w-full px-3 py-2.5 rounded-lg border border-border bg-card
                             text-dark text-sm focus:outline-none focus:ring-2
                             focus:ring-navy/30 focus:border-navy">
                <option value="">Select brand</option>
                @if (brands$ | async; as brands) {
                  @for (brand of brands; track brand.id) {
                    <option [value]="brand.id">{{ brand.name }}</option>
                  }
                }
              </select>
            </div>
          </div>

          <!-- Price + MRP -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-dark mb-1.5">
                Selling Price (₹) <span class="text-red">*</span>
              </label>
              <input formControlName="price" type="number" min="1" placeholder="0"
                     class="w-full px-3 py-2.5 rounded-lg border bg-card text-dark text-sm
                            placeholder:text-muted focus:outline-none focus:ring-2
                            focus:ring-navy/30 focus:border-navy transition-colors"
                     [class.border-red]="price.invalid && price.touched"
                     [class.border-border]="!(price.invalid && price.touched)">
              @if (price.invalid && price.touched) {
                <p class="text-xs text-red mt-1">Valid selling price required.</p>
              }
            </div>

            <div>
              <label class="block text-sm font-medium text-dark mb-1.5">MRP (₹)</label>
              <input formControlName="mrp" type="number" min="1" placeholder="0 (optional)"
                     class="w-full px-3 py-2.5 rounded-lg border border-border bg-card
                            text-dark text-sm placeholder:text-muted focus:outline-none
                            focus:ring-2 focus:ring-navy/30 focus:border-navy transition-colors">
            </div>
          </div>

          <!-- Image URLs -->
          <div>
            <label class="block text-sm font-medium text-dark mb-1.5">Image URLs</label>
            <textarea formControlName="imageUrls" rows="3"
                      placeholder="One URL per line, e.g.&#10;https://example.com/img1.jpg"
                      class="w-full px-3 py-2.5 rounded-lg border border-border bg-card
                             text-dark text-sm placeholder:text-muted focus:outline-none
                             focus:ring-2 focus:ring-navy/30 focus:border-navy
                             transition-colors resize-none font-mono text-xs">
            </textarea>
            <p class="text-xs text-muted mt-1">Enter one image URL per line.</p>
          </div>

          <!-- Submit -->
          <div class="flex gap-3 pt-2">
            <button type="submit" [disabled]="submitting()"
                    class="flex-1 sm:flex-none sm:w-40 py-2.5 bg-dark text-white text-sm
                           font-semibold rounded-lg hover:bg-navy disabled:opacity-60
                           disabled:cursor-not-allowed transition-colors
                           focus:outline-none focus:ring-2 focus:ring-navy/40">
              {{ submitting() ? 'Listing…' : 'List Product' }}
            </button>
            <a routerLink="/seller/products"
               class="py-2.5 px-4 border border-border text-sm text-muted rounded-lg
                      hover:bg-bg transition-colors">
              Cancel
            </a>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class SellerProductFormComponent implements OnDestroy {
  private readonly sellerService = inject(SellerService);
  private readonly fb            = inject(FormBuilder);
  private readonly router        = inject(Router);
  private readonly destroy$      = new Subject<void>();

  readonly categories$ = this.sellerService.getCategories();
  readonly brands$     = this.sellerService.getBrands();

  readonly submitting  = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name:        ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    categoryId:  ['', [Validators.required]],
    brandId:     ['', [Validators.required]],
    price:       [0,  [Validators.required, Validators.min(1)]],
    mrp:         [0],
    imageUrls:   [''],
  });

  get name()        { return this.form.controls.name; }
  get description() { return this.form.controls.description; }
  get price()       { return this.form.controls.price; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const req: CreateSellerProductRequest = {
      name:        v.name,
      description: v.description,
      categoryId:  v.categoryId,
      brandId:     v.brandId,
      price:       v.price,
      mrp:         v.mrp > 0 ? v.mrp : null,
      imageUrls:   v.imageUrls
        ? v.imageUrls.split('\n').map((u) => u.trim()).filter(Boolean)
        : [],
    };

    this.submitting.set(true);
    this.submitError.set(null);

    this.sellerService.createProduct(req).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/seller/products']);
      },
      error: () => {
        this.submitting.set(false);
        this.submitError.set('Failed to list product. Please try again.');
      },
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
