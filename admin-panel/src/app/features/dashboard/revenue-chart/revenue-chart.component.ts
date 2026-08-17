import { ChangeDetectionStrategy, Component, Input, OnChanges } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RevenueData } from '../../../core/services/admin-api.service';

interface Bar { date: string; revenue: number; heightPct: number; }

@Component({
  selector: 'app-revenue-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
      <div class="px-5 py-4 border-b border-border">
        <h2 class="font-semibold text-dark text-sm">Revenue Trend — Last 30 Days</h2>
        <p class="text-xs text-muted mt-0.5">Daily revenue in INR</p>
      </div>
      <div class="p-4">
        @if (bars.length === 0) {
          <div class="h-40 flex items-center justify-center text-muted text-sm">No data available</div>
        } @else {
          <div class="flex items-end gap-0.5 h-40 w-full">
            @for (bar of bars; track bar.date) {
              <div
                class="flex-1 bg-navy/80 hover:bg-navy rounded-t transition-colors cursor-default relative group"
                [style.height.%]="bar.heightPct || 2">
                <div class="absolute -top-8 left-1/2 -translate-x-1/2 bg-dark text-white text-xs px-2 py-1 rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity z-10 pointer-events-none">
                  ₹{{ bar.revenue | number }}
                </div>
              </div>
            }
          </div>
          <div class="flex justify-between mt-1 text-xs text-muted">
            <span>{{ bars[0]?.date }}</span>
            <span>{{ bars[bars.length - 1]?.date }}</span>
          </div>
        }
      </div>
    </div>
  `,
})
export class RevenueChartComponent implements OnChanges {
  @Input() data: RevenueData[] = [];

  bars: Bar[] = [];

  ngOnChanges(): void {
    if (!this.data.length) { this.bars = []; return; }
    const sorted = [...this.data].sort((a, b) => a.date.localeCompare(b.date));
    const max = Math.max(...sorted.map((d) => d.revenue), 1);
    this.bars = sorted.map((d) => ({
      date:      new Date(d.date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      revenue:   d.revenue,
      heightPct: Math.round((d.revenue / max) * 100),
    }));
  }
}
