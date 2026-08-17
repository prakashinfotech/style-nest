import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { SellerApiService, SellerAnalytics } from '../../../core/services/seller-api.service';
import { KpiCardComponent } from '../../../shared/components/kpi-card/kpi-card.component';

interface ProductBar {
  name: string;
  unitsSold: number;
  revenue: number;
  pct: number;
}

@Component({
  selector: 'app-seller-analytics',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DecimalPipe, KpiCardComponent],
  template: `
    <div class="space-y-6">
      <h1 class="text-xl font-bold text-dark">Analytics</h1>

      @if (analytics$ | async; as a) {
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <app-kpi-card label="Total Revenue"   [value]="'₹' + (a.totalRevenue | number:'1.0-0')"       icon="₹"  iconBg="bg-gold/10"  />
          <app-kpi-card label="Total Orders"    [value]="a.totalOrders.toString()"                       icon="📦" iconBg="bg-navy/10" />
          <app-kpi-card label="Avg Order Value" [value]="'₹' + (a.averageOrderValue | number:'1.0-2')" icon="📊" iconBg="bg-blue/10"  />
        </div>

        @if (a.topProducts.length) {
          <!-- CSS Bar Chart -->
          <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
            <div class="px-5 py-4 border-b border-border">
              <h2 class="font-semibold text-dark text-sm">Top Products by Revenue</h2>
            </div>
            <div class="p-5 space-y-3">
              @for (bar of buildBars(a); track bar.name) {
                <div>
                  <div class="flex justify-between mb-1">
                    <span class="text-xs font-medium text-dark truncate max-w-[60%]">{{ bar.name }}</span>
                    <span class="text-xs text-muted">₹{{ bar.revenue | number:'1.0-0' }} ({{ bar.unitsSold }} units)</span>
                  </div>
                  <div class="w-full bg-border rounded-full h-2">
                    <div class="h-2 rounded-full bg-gold transition-all" [style.width.%]="bar.pct"></div>
                  </div>
                </div>
              }
            </div>
          </div>

          <!-- Detail Table -->
          <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
            <div class="px-5 py-4 border-b border-border">
              <h2 class="font-semibold text-dark text-sm">Top Products — Detail</h2>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead>
                  <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                    <th class="px-5 py-3 text-left">#</th>
                    <th class="px-5 py-3 text-left">Product</th>
                    <th class="px-5 py-3 text-center">Units Sold</th>
                    <th class="px-5 py-3 text-right">Revenue</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of a.topProducts; track p.productName; let i = $index) {
                    <tr class="border-b border-border/50 hover:bg-bg/30">
                      <td class="px-5 py-3 text-muted font-mono text-xs">{{ i + 1 }}</td>
                      <td class="px-5 py-3 font-medium text-dark">{{ p.productName }}</td>
                      <td class="px-5 py-3 text-center text-muted">{{ p.unitsSold }}</td>
                      <td class="px-5 py-3 text-right font-medium text-gold">₹{{ p.revenue | number:'1.0-0' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">No product data yet.</div>
        }
      } @else {
        <div class="p-8 text-center text-muted text-sm">Loading analytics…</div>
      }
    </div>
  `,
})
export class SellerAnalyticsComponent implements OnInit {
  private readonly api = inject(SellerApiService);

  analytics$: Observable<SellerAnalytics> | null = null;

  ngOnInit(): void {
    this.analytics$ = this.api.getAnalytics().pipe(
      catchError(() => of({ totalRevenue: 0, totalOrders: 0, averageOrderValue: 0, topProducts: [] })),
    );
  }

  buildBars(a: SellerAnalytics): ProductBar[] {
    const max = Math.max(...a.topProducts.map((p) => p.revenue), 1);
    return a.topProducts.map((p) => ({
      name:      p.productName,
      unitsSold: p.unitsSold,
      revenue:   p.revenue,
      pct:       Math.round((p.revenue / max) * 100),
    }));
  }
}
