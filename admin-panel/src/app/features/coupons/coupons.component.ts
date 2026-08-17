import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

interface CouponDto { id: string; code: string; discountType: string; discountValue: number; isActive: boolean; expiresAt: string | null; usageCount: number; }

@Component({
  selector: 'app-coupons',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Coupon Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">All Coupons</h2>
        </div>
        @if (coupons$ | async; as coupons) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Code</th>
                  <th class="px-5 py-3 text-left">Type</th>
                  <th class="px-5 py-3 text-right">Value</th>
                  <th class="px-5 py-3 text-center">Used</th>
                  <th class="px-5 py-3 text-left">Expires</th>
                  <th class="px-5 py-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody>
                @for (c of coupons; track c.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-mono font-bold text-navy text-xs">{{ c.code }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ c.discountType }}</td>
                    <td class="px-5 py-3 text-right font-medium">{{ c.discountType === 'Percentage' ? c.discountValue + '%' : '₹' + c.discountValue }}</td>
                    <td class="px-5 py-3 text-center text-muted">{{ c.usageCount }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ c.expiresAt ? (c.expiresAt | date:'mediumDate') : '—' }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="c.isActive ? 'active' : 'suspended'" /></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading coupons…</div>
        }
      </div>
    </div>
  `,
})
export class CouponsComponent implements OnInit {
  coupons$: Observable<CouponDto[]> | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.coupons$ = this.http.get<CouponDto[]>('/api/v1/admin/coupons').pipe(catchError(() => of([])));
  }
}
