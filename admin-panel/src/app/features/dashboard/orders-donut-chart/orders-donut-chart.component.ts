import { ChangeDetectionStrategy, Component, Input, OnChanges } from '@angular/core';
import { DecimalPipe } from '@angular/common';

export interface StatusCount {
  status: string;
  count: number;
}

interface StatusBar { status: string; count: number; pct: number; color: string; }

const STATUS_COLORS: Record<string, string> = {
  Pending:   'bg-gold',
  Confirmed: 'bg-blue',
  Shipped:   'bg-navy',
  Delivered: 'bg-success',
  Cancelled: 'bg-red',
};

@Component({
  selector: 'app-orders-donut-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
      <div class="px-5 py-4 border-b border-border">
        <h2 class="font-semibold text-dark text-sm">Orders by Status</h2>
        <p class="text-xs text-muted mt-0.5">Distribution of order statuses</p>
      </div>
      <div class="p-5 space-y-3">
        @for (item of bars; track item.status) {
          <div>
            <div class="flex justify-between mb-1">
              <span class="text-xs font-medium text-dark">{{ item.status }}</span>
              <span class="text-xs text-muted">{{ item.count | number }} ({{ item.pct }}%)</span>
            </div>
            <div class="w-full bg-border rounded-full h-2">
              <div class="h-2 rounded-full transition-all" [class]="item.color" [style.width.%]="item.pct"></div>
            </div>
          </div>
        }
        @if (bars.length === 0) {
          <p class="text-muted text-sm text-center py-4">No data available</p>
        }
      </div>
    </div>
  `,
})
export class OrdersDonutChartComponent implements OnChanges {
  @Input() data: StatusCount[] = [];

  bars: StatusBar[] = [];

  ngOnChanges(): void {
    const total = this.data.reduce((s, d) => s + d.count, 0) || 1;
    this.bars = this.data.map((d) => ({
      status: d.status,
      count:  d.count,
      pct:    Math.round((d.count / total) * 100),
      color:  STATUS_COLORS[d.status] ?? 'bg-mid-gray',
    }));
  }
}
