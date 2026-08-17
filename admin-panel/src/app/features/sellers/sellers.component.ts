import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { BehaviorSubject, Observable, catchError, of, switchMap } from 'rxjs';
import { AdminApiService, SellerSummary } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-sellers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-dark">Seller Management</h1>
        <div class="flex gap-2">
          @for (filter of filters; track filter.value) {
            <button
              (click)="setFilter(filter.value)"
              class="text-xs px-3 py-1.5 rounded-full border transition-colors"
              [class.bg-navy]="(filter$ | async) === filter.value"
              [class.text-white]="(filter$ | async) === filter.value"
              [class.border-navy]="(filter$ | async) === filter.value"
              [class.border-border]="(filter$ | async) !== filter.value"
              [class.text-muted]="(filter$ | async) !== filter.value">
              {{ filter.label }}
            </button>
          }
        </div>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        @if (sellers$ | async; as sellers) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Store Name</th>
                  <th class="px-5 py-3 text-left">Email</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-center">Commission</th>
                  <th class="px-5 py-3 text-center">Products</th>
                  <th class="px-5 py-3 text-left">Joined</th>
                  <th class="px-5 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (s of sellers; track s.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ s.storeName }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ s.email }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="s.status" /></td>
                    <td class="px-5 py-3 text-center text-muted">{{ s.commissionRate }}%</td>
                    <td class="px-5 py-3 text-center text-muted">{{ s.productCount }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ s.createdAt | date:'mediumDate' }}</td>
                    <td class="px-5 py-3 text-center">
                      @if (s.status === 'Pending') {
                        <div class="flex gap-1 justify-center">
                          <button (click)="approve(s.id)" class="text-xs px-2 py-1 bg-success text-white rounded hover:bg-success/80 transition-colors">Approve</button>
                          <button (click)="reject(s.id)" class="text-xs px-2 py-1 bg-error text-white rounded hover:bg-error/80 transition-colors">Reject</button>
                        </div>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (!sellers.length) {
            <div class="p-8 text-center text-muted text-sm">No sellers found for this filter.</div>
          }
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading sellers…</div>
        }
      </div>
    </div>
  `,
})
export class SellersComponent implements OnInit {
  filters = [
    { label: 'All',     value: '' },
    { label: 'Pending', value: 'Pending' },
    { label: 'Active',  value: 'Active' },
    { label: 'Rejected', value: 'Rejected' },
  ];

  filter$ = new BehaviorSubject<string>('');
  sellers$: Observable<SellerSummary[]> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.sellers$ = this.filter$.pipe(
      switchMap((f) => this.api.getSellers(f || undefined).pipe(catchError(() => of([]))))
    );
  }

  setFilter(v: string): void { this.filter$.next(v); }

  approve(id: string): void {
    this.api.approveSeller(id, true).subscribe(() => this.filter$.next(this.filter$.value));
  }

  reject(id: string): void {
    const reason = prompt('Rejection reason:');
    if (reason !== null) {
      this.api.approveSeller(id, false, reason).subscribe(() => this.filter$.next(this.filter$.value));
    }
  }
}
