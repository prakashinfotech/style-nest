import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import {
  AttributeDefinition,
  BrandOption,
  CategoryOption,
  CreateProductRequest,
  SellerApiService,
  SellerProductDetail,
} from '../../../core/services/seller-api.service';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload.component';

@Component({
  selector: 'app-seller-product-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FileUploadComponent],
  template: `
    <div class="max-w-3xl space-y-6">
      <div>
        <h1 class="text-xl font-bold text-dark">{{ isEdit() ? 'Edit Product' : 'Create Product' }}</h1>
        <p class="text-sm text-muted mt-0.5">
          {{ isEdit() ? 'Update your product details and attributes.' : 'List a new product in your store.' }}
        </p>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-20">
          <div class="animate-spin rounded-full h-8 w-8 border-2 border-navy border-t-transparent"></div>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-6">

          <!-- Basic Info -->
          <div class="bg-white rounded-xl shadow-sm border border-border p-5 space-y-4">
            <h2 class="font-semibold text-dark text-sm border-b border-border pb-2">Basic Information</h2>

            <div>
              <label class="block text-xs font-medium text-dark mb-1">Product Name <span class="text-red">*</span></label>
              <input type="text" formControlName="name"
                class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                placeholder="e.g. Slim Fit Cotton Shirt"
              />
              @if (form.get('name')?.invalid && form.get('name')?.touched) {
                <p class="text-xs text-red mt-1">Product name is required.</p>
              }
            </div>

            <div>
              <label class="block text-xs font-medium text-dark mb-1">Description <span class="text-red">*</span></label>
              <textarea formControlName="description" rows="4"
                class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50 resize-none"
                placeholder="Describe the product in detail…"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-dark mb-1">Base Price (₹) <span class="text-red">*</span></label>
                <input type="number" formControlName="basePrice" min="0"
                  class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-dark mb-1">Sale Price (₹)</label>
                <input type="number" formControlName="discountedPrice" min="0"
                  class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                  placeholder="Leave blank if no discount"
                />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-dark mb-1">Category <span class="text-red">*</span></label>
                <select formControlName="categoryId"
                  class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                  (change)="onCategoryChange()"
                >
                  <option value="">Select category</option>
                  @for (cat of categories(); track cat.id) {
                    <option [value]="cat.id">
                      {{ cat.parentName ? cat.parentName + ' › ' : '' }}{{ cat.name }}
                    </option>
                  }
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-dark mb-1">Brand <span class="text-red">*</span></label>
                <select formControlName="brandId"
                  class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                >
                  <option value="">Select brand</option>
                  @for (brand of brands(); track brand.id) {
                    <option [value]="brand.id">{{ brand.name }}</option>
                  }
                </select>
              </div>
            </div>
          </div>

          <!-- Images -->
          <div class="bg-white rounded-xl shadow-sm border border-border p-5 space-y-3">
            <h2 class="font-semibold text-dark text-sm border-b border-border pb-2">Product Images</h2>
            <app-file-upload
              accept="image/*"
              [multiple]="true"
              [maxSizeMb]="5"
              hint="PNG, JPG up to 5 MB each — first image is the main display"
              (filesChange)="onFilesChange($event)"
            />
          </div>

          <!-- Dynamic Attributes -->
          @if (attributes().length) {
            <div class="bg-white rounded-xl shadow-sm border border-border p-5 space-y-4">
              <h2 class="font-semibold text-dark text-sm border-b border-border pb-2">
                Category Attributes
              </h2>
              <div formArrayName="attributes" class="space-y-3">
                @for (attr of attributes(); track attr.id; let i = $index) {
                  <div [formGroupName]="i">
                    <label class="block text-xs font-medium text-dark mb-1">
                      {{ attr.name }}
                      @if (attr.isRequired) { <span class="text-red">*</span> }
                    </label>
                    @if (attr.options?.length) {
                      <select formControlName="value"
                        class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                      >
                        <option value="">Select {{ attr.name }}</option>
                        @for (opt of attr.options!; track opt) {
                          <option [value]="opt">{{ opt }}</option>
                        }
                      </select>
                    } @else {
                      <input type="text" formControlName="value"
                        class="w-full border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                        [placeholder]="'Enter ' + attr.name"
                      />
                    }
                  </div>
                }
              </div>
            </div>
          }

          <!-- Variants -->
          <div class="bg-white rounded-xl shadow-sm border border-border p-5 space-y-4">
            <div class="flex items-center justify-between border-b border-border pb-2">
              <h2 class="font-semibold text-dark text-sm">Variants</h2>
              <button type="button"
                class="text-xs text-navy font-medium hover:underline"
                (click)="addVariant()"
              >+ Add Variant</button>
            </div>

            <div formArrayName="variants" class="space-y-3">
              @for (v of variantsArray.controls; track $index; let vi = $index) {
                <div [formGroupName]="vi"
                  class="grid grid-cols-2 sm:grid-cols-4 gap-3 p-3 bg-bg rounded-lg relative"
                >
                  <div>
                    <label class="block text-[10px] font-medium text-muted mb-1">Size <span class="text-red">*</span></label>
                    <input type="text" formControlName="size"
                      class="w-full border border-border rounded px-2 py-1.5 text-xs text-dark focus:outline-none focus:ring-1 focus:ring-navy/50"
                      placeholder="e.g. M"
                    />
                  </div>
                  <div>
                    <label class="block text-[10px] font-medium text-muted mb-1">Colour</label>
                    <input type="text" formControlName="colour"
                      class="w-full border border-border rounded px-2 py-1.5 text-xs text-dark focus:outline-none focus:ring-1 focus:ring-navy/50"
                      placeholder="e.g. Navy"
                    />
                  </div>
                  <div>
                    <label class="block text-[10px] font-medium text-muted mb-1">Stock <span class="text-red">*</span></label>
                    <input type="number" formControlName="stockQuantity" min="0"
                      class="w-full border border-border rounded px-2 py-1.5 text-xs text-dark focus:outline-none focus:ring-1 focus:ring-navy/50"
                    />
                  </div>
                  <div>
                    <label class="block text-[10px] font-medium text-muted mb-1">Price Override (₹)</label>
                    <input type="number" formControlName="priceOverride" min="0"
                      class="w-full border border-border rounded px-2 py-1.5 text-xs text-dark focus:outline-none focus:ring-1 focus:ring-navy/50"
                      placeholder="Optional"
                    />
                  </div>
                  @if (variantsArray.length > 1) {
                    <button type="button"
                      class="absolute top-2 right-2 w-5 h-5 rounded-full bg-red/10 text-red text-xs flex items-center justify-center hover:bg-red/20"
                      (click)="removeVariant(vi)"
                      aria-label="Remove variant"
                    >✕</button>
                  }
                </div>
              }
            </div>
          </div>

          <!-- Submit -->
          <div class="flex gap-3">
            <button
              type="submit"
              class="px-6 py-2.5 bg-navy text-white text-sm font-medium rounded-lg hover:bg-navy/90 transition-colors disabled:opacity-50"
              [disabled]="submitting() || form.invalid"
            >
              {{ submitting() ? (isEdit() ? 'Saving…' : 'Creating…') : (isEdit() ? 'Save Changes' : 'Create Product') }}
            </button>
            <button
              type="button"
              class="px-6 py-2.5 border border-border text-dark text-sm rounded-lg hover:bg-bg transition-colors"
              (click)="cancel()"
            >Cancel</button>
          </div>

          @if (errorMsg()) {
            <p class="text-sm text-red">{{ errorMsg() }}</p>
          }
        </form>
      }
    </div>
  `,
})
export class SellerProductFormComponent implements OnInit {
  isEdit = signal(false);
  loading = signal(false);
  submitting = signal(false);
  errorMsg = signal('');

  categories = signal<CategoryOption[]>([]);
  brands = signal<BrandOption[]>([]);
  attributes = signal<AttributeDefinition[]>([]);

  private productId: string | null = null;
  private uploadedFiles: File[] = [];

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private api: SellerApiService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.form = this.fb.group({
      name:            ['', Validators.required],
      description:     ['', Validators.required],
      basePrice:       [null, [Validators.required, Validators.min(0)]],
      discountedPrice: [null],
      categoryId:      ['', Validators.required],
      brandId:         ['', Validators.required],
      attributes:      this.fb.array([]),
      variants:        this.fb.array([this.createVariantGroup()]),
    });
  }

  get variantsArray(): FormArray {
    return this.form.get('variants') as FormArray;
  }

  get attributesArray(): FormArray {
    return this.form.get('attributes') as FormArray;
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.productId);

    this.loading.set(true);

    const cats$ = this.api.getCategories().pipe(catchError(() => of([] as CategoryOption[])));
    const brands$ = this.api.getBrands().pipe(catchError(() => of([] as BrandOption[])));

    cats$.subscribe((cats) => this.categories.set(cats));
    brands$.subscribe((brands) => this.brands.set(brands));

    if (this.productId) {
      this.api.getProductById(this.productId)
        .pipe(catchError(() => of(null)))
        .subscribe((product) => {
          if (product) this.patchForm(product);
          this.loading.set(false);
        });
    } else {
      this.loading.set(false);
    }
  }

  onCategoryChange(): void {
    const catId = this.form.value.categoryId as string;
    if (!catId) {
      this.attributes.set([]);
      this.attributesArray.clear();
      return;
    }

    this.api.getCategoryAttributes(catId)
      .pipe(catchError(() => of([] as AttributeDefinition[])))
      .subscribe((defs) => {
        this.attributes.set(defs);
        this.attributesArray.clear();
        defs.forEach((def) => {
          this.attributesArray.push(
            this.fb.group({
              attributeId: [def.id],
              value: ['', def.isRequired ? Validators.required : []],
            }),
          );
        });
      });
  }

  addVariant(): void {
    this.variantsArray.push(this.createVariantGroup());
  }

  removeVariant(index: number): void {
    this.variantsArray.removeAt(index);
  }

  onFilesChange(files: File[]): void {
    this.uploadedFiles = files;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMsg.set('');

    const payload: CreateProductRequest = {
      ...this.form.value,
      images: this.uploadedFiles.map((f) => URL.createObjectURL(f)),
    };

    const req$ = this.productId
      ? this.api.updateProduct(this.productId, payload)
      : this.api.createProduct(payload);

    req$.pipe(catchError((err) => {
      this.errorMsg.set(err?.error?.message ?? 'Failed to save product. Please try again.');
      this.submitting.set(false);
      return of(null);
    })).subscribe((result) => {
      if (result) {
        this.router.navigate(['/seller/products']);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/seller/products']);
  }

  private createVariantGroup(): FormGroup {
    return this.fb.group({
      size:           ['', Validators.required],
      colour:         [''],
      stockQuantity:  [0, [Validators.required, Validators.min(0)]],
      priceOverride:  [null],
    });
  }

  private patchForm(product: SellerProductDetail): void {
    this.form.patchValue({
      name:            product.name,
      description:     product.description,
      basePrice:       product.basePrice,
      discountedPrice: product.discountedPrice,
      categoryId:      product.categoryId,
      brandId:         product.brandId,
    });

    this.variantsArray.clear();
    (product.variants ?? []).forEach((v) => {
      this.variantsArray.push(
        this.fb.group({
          size:          [v.size, Validators.required],
          colour:        [v.colour],
          stockQuantity: [v.stockQuantity, [Validators.required, Validators.min(0)]],
          priceOverride: [v.priceOverride],
        }),
      );
    });

    if (product.categoryId) {
      this.api.getCategoryAttributes(product.categoryId)
        .pipe(catchError(() => of([] as AttributeDefinition[])))
        .subscribe((defs) => {
          this.attributes.set(defs);
          this.attributesArray.clear();
          defs.forEach((def) => {
            const existing = product.attributes.find((a) => a.attributeId === def.id);
            this.attributesArray.push(
              this.fb.group({
                attributeId: [def.id],
                value: [existing?.value ?? '', def.isRequired ? Validators.required : []],
              }),
            );
          });
        });
    }
  }
}
