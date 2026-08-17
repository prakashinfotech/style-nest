import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BehaviorSubject, Observable, catchError, of, switchMap } from 'rxjs';
import { SellerApiService, InventoryItem, PagedResult } from '../../../core/services/seller-api.service';

@Component({
  selector: 'app-seller-inventory',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, ReactiveFormsModule],
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-dark">Inventory</h1>
        <span class="text-xs text-muted">
          @if (result$ | async; as r) { {{ r.totalCount }} variants }
        </span>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        @if (result$ | async; as r) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Product</th>
                  <th class="px-5 py-3 text-left">Size</th>
                  <th class="px-5 py-3 text-left">Colour</th>
                  <th class="px-5 py-3 text-right">Price</th>
                  <th class="px-5 py-3 text-center">Stock</th>
                  <th class="px-5 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (item of r.items; track item.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ item.productName }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ item.size }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ item.colour ?? '—' }}</td>
                    <td class="px-5 py-3 text-right font-medium">₹{{ item.price }}</td>
                    <td class="px-5 py-3 text-center">
                      <span [class]="item.stock <= item.lowStockThreshold ? 'text-error font-semibold' : 'text-muted'">
                        {{ item.stock }}
                      </span>
                      @if (item.stock <= item.lowStockThreshold) {
                        <span class="ml-1 text-xs text-error">(low)</span>
                      }
                    </td>
                    <td class="px-5 py-3 text-center">
                      @if (editingId() === item.id) {
                        <form [formGroup]="editForm" (ngSubmit)="saveEdit(item.id)" class="flex gap-1 justify-center">
                          <input formControlName="stock" type="number" min="0" placeholder="Stock"
                            class="w-16 border border-border rounded px-1 py-0.5 text-xs text-center focus:outline-none focus:border-navy" />
                          <input formControlName="price" type="number" min="0" placeholder="Price"
                            class="w-20 border border-border rounded px-1 py-0.5 text-xs text-center focus:outline-none focus:border-navy" />
                          <button type="submit" [disabled]="editForm.invalid"
                            class="text-xs px-2 py-0.5 bg-success text-white rounded disabled:opacity-50">Save</button>
                          <button type="button" (click)="cancelEdit()"
                            class="text-xs px-2 py-0.5 border border-border rounded">✕</button>
                        </form>
                      } @else {
                        <button (click)="startEdit(item)"
                          class="text-xs px-2 py-1 border border-border rounded hover:bg-bg transition-colors">Edit</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (!r.items.length) {
            <div class="p-8 text-center text-muted text-sm">No inventory items found.</div>
          }
          <div class="px-5 py-3 border-t border-border flex items-center justify-between text-xs text-muted">
            <span>Page {{ page }} of {{ Math.ceil(r.totalCount / pageSize) || 1 }}</span>
            <div class="flex gap-2">
              <button (click)="prevPage()" [disabled]="page === 1"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Prev</button>
              <button (click)="nextPage()" [disabled]="page * pageSize >= r.totalCount"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Next</button>
            </div>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading inventory…</div>
        }
      </div>
    </div>
  `,
})
export class SellerInventoryComponent implements OnInit {
  page = 1;
  pageSize = 20;
  Math = Math;

  editingId = signal<string | null>(null);
  editForm: FormGroup;

  page$ = new BehaviorSubject<number>(1);
  result$: Observable<PagedResult<InventoryItem>> | null = null;

  constructor(private api: SellerApiService, private fb: FormBuilder) {
    this.editForm = this.fb.group({
      stock: [0, [Validators.required, Validators.min(0)]],
      price: [0, [Validators.required, Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    this.result$ = this.page$.pipe(
      switchMap((p) => this.api.getInventory(p, this.pageSize).pipe(
        catchError(() => of({ items: [], totalCount: 0, page: p, pageSize: this.pageSize }))
      ))
    );
  }

  startEdit(item: InventoryItem): void {
    this.editingId.set(item.id);
    this.editForm.setValue({ stock: item.stock, price: item.price });
  }

  cancelEdit(): void { this.editingId.set(null); }

  saveEdit(id: string): void {
    if (this.editForm.invalid) return;
    const { stock, price } = this.editForm.getRawValue();
    this.api.updateInventory(id, stock, price).subscribe(() => {
      this.editingId.set(null);
      this.page$.next(this.page);
    });
  }

  prevPage(): void { if (this.page > 1) { this.page--; this.page$.next(this.page); } }
  nextPage(): void { this.page++; this.page$.next(this.page); }
}
