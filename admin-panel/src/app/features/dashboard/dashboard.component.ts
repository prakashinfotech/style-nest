import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { AdminApiService, DashboardMetrics, RevenueData } from '../../core/services/admin-api.service';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { RevenueChartComponent } from './revenue-chart/revenue-chart.component';
import { OrdersDonutChartComponent, StatusCount } from './orders-donut-chart/orders-donut-chart.component';
import { UserRegistrationChartComponent, DailyCount } from './user-registration-chart/user-registration-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AsyncPipe, DecimalPipe,
    KpiCardComponent,
    RevenueChartComponent,
    OrdersDonutChartComponent,
    UserRegistrationChartComponent,
  ],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-xl font-bold text-dark">Dashboard</h1>
        <p class="text-sm text-muted mt-0.5">Platform overview</p>
      </div>

      @if (metrics$ | async; as m) {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <app-kpi-card label="Total Orders"    [value]="(m.totalOrders | number) ?? ''"      icon="📦" iconBg="bg-blue/10" />
          <app-kpi-card label="Total Revenue"   [value]="'₹' + (m.totalRevenue | number:'1.0-0')" icon="💰" iconBg="bg-success/10" />
          <app-kpi-card label="Registered Users" [value]="(m.totalUsers | number) ?? ''"     icon="👥" iconBg="bg-navy/10" />
          <app-kpi-card label="Active Products"  [value]="(m.totalProducts | number) ?? ''"  icon="👗" iconBg="bg-gold/10" />
          <app-kpi-card label="Total Sellers"    [value]="(m.totalSellers | number) ?? ''"   icon="🏪" iconBg="bg-navy/10" [subtitle]="m.pendingSellers + ' pending approval'" />
          <app-kpi-card label="Brands"           [value]="(m.totalBrands | number) ?? ''"    icon="🏷️"  iconBg="bg-mid-gray/20" />
          <app-kpi-card label="Categories"       [value]="(m.totalCategories | number) ?? ''" icon="🗂️"  iconBg="bg-mid-gray/20" />
        </div>
      } @else {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          @for (i of [1,2,3,4,5,6,7]; track i) {
            <div class="bg-white rounded-xl p-5 shadow-sm border border-border animate-pulse h-24"></div>
          }
        </div>
      }

      <!-- Revenue chart -->
      @if (revenue$ | async; as revenue) {
        <app-revenue-chart [data]="revenue" />
      }

      <!-- Bottom charts row -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Orders by status donut -->
        <app-orders-donut-chart [data]="orderStatusData" />

        <!-- User registrations area chart -->
        @if (revenue$ | async; as revenue) {
          <app-user-registration-chart [data]="mockRegistrations(revenue)" />
        }
      </div>
    </div>
  `,
})
export class DashboardComponent implements OnInit {
  metrics$: Observable<DashboardMetrics> | null = null;
  revenue$: Observable<RevenueData[]>   | null = null;

  readonly orderStatusData: StatusCount[] = [
    { status: 'Pending',    count: 142 },
    { status: 'Confirmed',  count: 318 },
    { status: 'Shipped',    count: 204 },
    { status: 'Delivered',  count: 876 },
    { status: 'Cancelled',  count: 53  },
  ];

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.metrics$ = this.api.getMetrics().pipe(catchError(() => of({} as DashboardMetrics)));
    this.revenue$ = this.api.getRevenue(30).pipe(catchError(() => of([])));
  }

  mockRegistrations(revenue: RevenueData[]): DailyCount[] {
    return revenue.map((r) => ({ date: r.date, count: Math.round(r.orderCount * 0.4 + Math.random() * 5) }));
  }
}
