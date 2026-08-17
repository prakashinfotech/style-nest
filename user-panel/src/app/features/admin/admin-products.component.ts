import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminProduct,
  AdminService,
  GenerateDescriptionResponse,
} from '../../core/services/admin.service';

interface AiDescriptionState {
  productId: string;
  productName: string;
  loading: boolean;
  result: GenerateDescriptionResponse | null;
  error: string | null;
  copied: boolean;
}

@Component({
  selector: 'app-admin-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe],
  template: `
    <div class="p-4 md:p-8">
      <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">Products</h1>

      @if (products$ | async; as products) {
        <p class="text-sm text-muted mb-4">{{ products.length }} products in catalog</p>

        @if (products.length === 0) {
          <div class="bg-card rounded-xl border border-border p-12 text-center text-muted">
            No products in catalog.
          </div>
        } @else {
          <div class="bg-card rounded-xl border border-border overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-bg border-b border-border">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Product</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Brand</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Category</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Price</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Stock</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden lg:table-cell">Status</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">AI</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border">
                  @for (product of products; track product.id) {
                    <tr class="hover:bg-bg/50 transition-colors">
                      <td class="px-4 py-3">
                        <p class="font-medium text-dark line-clamp-1">{{ product.name }}</p>
                        <p class="text-xs text-muted md:hidden">{{ product.brandName }}</p>
                      </td>
                      <td class="px-4 py-3 text-muted hidden md:table-cell">{{ product.brandName }}</td>
                      <td class="px-4 py-3 text-muted hidden md:table-cell">{{ product.categoryName }}</td>
                      <td class="px-4 py-3 font-semibold text-dark">
                        ₹{{ product.price.toLocaleString('en-IN') }}
                      </td>
                      <td class="px-4 py-3">
                        <span [class]="product.inStock ? 'text-xs text-success' : 'text-xs text-red'">
                          {{ product.inStock ? 'In Stock' : 'Out' }}
                        </span>
                      </td>
                      <td class="px-4 py-3 hidden lg:table-cell">
                        <span [class]="product.isActive
                          ? 'text-xs px-2 py-0.5 rounded-full bg-success/10 text-success'
                          : 'text-xs px-2 py-0.5 rounded-full bg-muted/10 text-muted'">
                          {{ product.isActive ? 'Active' : 'Inactive' }}
                        </span>
                      </td>
                      <!-- ENH-AI-003 — Generate Description button -->
                      <td class="px-4 py-3">
                        <button
                          (click)="generateDescription(product)"
                          [disabled]="aiState()?.productId === product.id && aiState()?.loading"
                          class="text-xs px-2 py-1 rounded bg-blue/10 text-blue hover:bg-blue/20 transition-colors disabled:opacity-50 disabled:cursor-not-allowed whitespace-nowrap"
                          title="Generate AI description">
                          @if (aiState()?.productId === product.id && aiState()?.loading) {
                            <span>Generating…</span>
                          } @else {
                            <span>✨ AI Desc</span>
                          }
                        </button>
                      </td>
                    </tr>

                    <!-- AI Result panel — shown inline below the row -->
                    @if (aiState()?.productId === product.id && !aiState()?.loading) {
                      <tr>
                        <td colspan="7" class="px-4 py-3 bg-blue/5 border-t border-blue/10">
                          @if (aiState()?.error) {
                            <div class="flex items-start gap-2 text-red text-sm">
                              <span>⚠</span>
                              <span>{{ aiState()?.error }}</span>
                            </div>
                          } @else if (aiState()?.result) {
                            <div class="space-y-2">
                              <div class="flex items-center justify-between gap-4 flex-wrap">
                                <p class="text-xs font-semibold text-blue uppercase tracking-wider">
                                  ✨ AI-Generated Description
                                </p>
                                <div class="flex gap-2">
                                  <button
                                    (click)="copyDescription()"
                                    class="text-xs px-2 py-1 rounded border border-blue/30 text-blue hover:bg-blue/10 transition-colors">
                                    {{ aiState()?.copied ? '✓ Copied' : 'Copy' }}
                                  </button>
                                  <button
                                    (click)="clearAiState()"
                                    class="text-xs px-2 py-1 rounded border border-border text-muted hover:bg-bg transition-colors">
                                    Dismiss
                                  </button>
                                </div>
                              </div>
                              <p class="text-sm text-dark leading-relaxed">{{ aiState()?.result?.description }}</p>
                              @if (aiState()?.result?.disclaimer) {
                                <p class="text-xs text-muted italic">{{ aiState()?.result?.disclaimer }}</p>
                              }
                            </div>
                          }
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      } @else {
        <div class="space-y-2">
          @for (i of [1, 2, 3, 4, 5, 6]; track i) {
            <div class="h-12 bg-border/40 rounded-lg animate-pulse"></div>
          }
        </div>
      }
    </div>
  `,
})
export class AdminProductsComponent {
  private readonly adminService = inject(AdminService);
  readonly products$: Observable<AdminProduct[]> = this.adminService.getAdminProducts();

  // ENH-AI-003 — signal-based state for the active AI description panel
  readonly aiState = signal<AiDescriptionState | null>(null);

  generateDescription(product: AdminProduct): void {
    this.aiState.set({
      productId:   product.id,
      productName: product.name,
      loading:     true,
      result:      null,
      error:       null,
      copied:      false,
    });

    this.adminService
      .generateProductDescription({
        productName: product.name,
        category:    product.categoryName,
        brand:       product.brandName,
        price:       product.price,
      })
      .subscribe({
        next: (res) =>
          this.aiState.update((s) =>
            s ? { ...s, loading: false, result: res } : null,
          ),
        error: (err) =>
          this.aiState.update((s) =>
            s
              ? {
                  ...s,
                  loading: false,
                  error:   err?.error?.message ?? 'Failed to generate description. Please try again.',
                }
              : null,
          ),
      });
  }

  copyDescription(): void {
    const desc = this.aiState()?.result?.description;
    if (!desc) return;
    navigator.clipboard.writeText(desc).then(() => {
      this.aiState.update((s) => (s ? { ...s, copied: true } : null));
      setTimeout(() => this.aiState.update((s) => (s ? { ...s, copied: false } : null)), 2000);
    });
  }

  clearAiState(): void {
    this.aiState.set(null);
  }
}
