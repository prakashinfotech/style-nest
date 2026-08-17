import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AdminService, Coupon } from '../../core/services/admin.service';

@Component({
  selector: 'app-coupon-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, RouterLink],
  template: `
    <div class="min-h-screen bg-[#F5F5F5] p-4 md:p-8">
      <div class="max-w-5xl mx-auto">

        <div class="flex items-center justify-between mb-6">
          <div>
            <a routerLink="/admin" class="text-sm text-[#0071C2] hover:underline">← Dashboard</a>
            <h1 class="text-2xl font-bold text-[#1A1A6B] mt-1">Coupons</h1>
          </div>
        </div>

        @if (coupons$ | async; as coupons) {
          @if (coupons.length === 0) {
            <div class="bg-white rounded-lg shadow p-8 text-center text-[#757575]">
              No coupons yet.
            </div>
          } @else {
            <div class="overflow-x-auto bg-white rounded-lg shadow">
              <table class="w-full text-sm">
                <thead class="bg-[#1A1A6B] text-white text-left">
                  <tr>
                    <th class="px-4 py-3">Code</th>
                    <th class="px-4 py-3 hidden md:table-cell">Type</th>
                    <th class="px-4 py-3 hidden md:table-cell">Value</th>
                    <th class="px-4 py-3 hidden md:table-cell">Used</th>
                    <th class="px-4 py-3">Active</th>
                    <th class="px-4 py-3">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (c of coupons; track c.id) {
                    <tr class="border-b last:border-0 hover:bg-[#F5F5F5]">
                      <td class="px-4 py-3 font-mono font-semibold text-[#1A1A6B]">{{ c.code }}</td>
                      <td class="px-4 py-3 hidden md:table-cell text-[#757575]">{{ c.discountType }}</td>
                      <td class="px-4 py-3 hidden md:table-cell font-medium text-[#212121]">
                        {{ c.discountType === 'Percentage' ? c.discountValue + '%' : ('₹' + c.discountValue) }}
                      </td>
                      <td class="px-4 py-3 hidden md:table-cell text-[#757575]">
                        {{ c.usedCount }}{{ c.totalUsageLimit ? ' / ' + c.totalUsageLimit : '' }}
                      </td>
                      <td class="px-4 py-3">
                        <span [class]="c.isActive
                          ? 'bg-[#2E7D32]/10 text-[#2E7D32] text-xs font-medium px-2 py-0.5 rounded-full'
                          : 'bg-[#757575]/10 text-[#757575] text-xs font-medium px-2 py-0.5 rounded-full'">
                          {{ c.isActive ? 'Active' : 'Inactive' }}
                        </span>
                      </td>
                      <td class="px-4 py-3">
                        <button (click)="deleteCoupon(c.id)"
                                class="text-[#E4002B] hover:underline text-xs">
                          Delete
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        } @else {
          <div class="bg-white rounded-lg shadow p-8 text-center text-[#757575]">
            Loading coupons…
          </div>
        }
      </div>
    </div>
  `,
})
export class CouponListComponent {
  private readonly adminService = inject(AdminService);
  readonly coupons$: Observable<Coupon[]> = this.adminService.getCoupons();

  deleteCoupon(id: string): void {
    this.adminService.deleteCoupon(id).subscribe();
  }
}
