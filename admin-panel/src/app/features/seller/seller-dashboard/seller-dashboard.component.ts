import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { SellerApiService, SellerDashboard, SellerProfile } from '../../../core/services/seller-api.service';
import { KpiCardComponent } from '../../../shared/components/kpi-card/kpi-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DecimalPipe, KpiCardComponent, StatusBadgeComponent],
  template: `
    <div class="space-y-6">
      <h1 class="text-xl font-bold text-dark">Seller Dashboard</h1>

      @if (profile$ | async; as profile) {
        <div class="bg-white rounded-xl shadow-sm border border-border p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4">
          @if (profile.logoUrl) {
            <img [src]="profile.logoUrl" [alt]="profile.storeName" class="w-14 h-14 object-contain rounded-lg border border-border" />
          } @else {
            <div class="w-14 h-14 bg-navy/10 rounded-lg flex items-center justify-center text-navy font-bold text-xl">
              {{ profile.storeName[0] }}
            </div>
          }
          <div class="flex-1">
            <p class="font-semibold text-dark text-base">{{ profile.storeName }}</p>
            <p class="text-xs text-muted">{{ profile.description }}</p>
            <div class="flex gap-3 mt-1">
              <app-status-badge [status]="profile.status" />
              <span class="text-xs text-muted">Commission: <strong class="text-dark">{{ profile.commissionRate }}%</strong></span>
            </div>
          </div>
        </div>
      }

      @if (dashboard$ | async; as d) {
        <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
          <app-kpi-card label="Total Revenue" [value]="'₹' + (d.totalRevenue | number:'1.0-0')" icon="₹" iconBg="bg-gold/10" />
          <app-kpi-card label="Total Orders" [value]="d.totalOrders.toString()" icon="📦" iconBg="bg-navy/10" />
          <app-kpi-card label="Total Products" [value]="d.totalProducts.toString()" icon="🏷️" iconBg="bg-blue/10" />
          <app-kpi-card label="Pending Orders" [value]="d.pendingOrders.toString()" icon="⏳" iconBg="bg-red/10" />
          <app-kpi-card label="Revenue This Month" [value]="'₹' + (d.revenueThisMonth | number:'1.0-0')" icon="📈" iconBg="bg-success/10" />
          <app-kpi-card label="Orders This Month" [value]="d.ordersThisMonth.toString()" icon="🗓️" iconBg="bg-navy/10" />
        </div>
      } @else {
        <div class="p-8 text-center text-muted text-sm">Loading dashboard…</div>
      }
    </div>
  `,
})
export class SellerDashboardComponent implements OnInit {
  dashboard$: Observable<SellerDashboard> | null = null;
  profile$: Observable<SellerProfile> | null = null;

  constructor(private api: SellerApiService) {}

  ngOnInit(): void {
    this.dashboard$ = this.api.getDashboard().pipe(catchError(() => of({
      totalRevenue: 0, totalOrders: 0, totalProducts: 0,
      pendingOrders: 0, revenueThisMonth: 0, ordersThisMonth: 0,
    })));
    this.profile$ = this.api.getProfile().pipe(catchError(() => of(null as unknown as SellerProfile)));
  }
}
