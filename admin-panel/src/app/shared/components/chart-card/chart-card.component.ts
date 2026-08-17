import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-chart-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
      <div class="px-5 py-4 border-b border-border flex items-center justify-between">
        <div>
          <h3 class="font-semibold text-dark text-sm">{{ title }}</h3>
          @if (subtitle) {
            <p class="text-xs text-muted mt-0.5">{{ subtitle }}</p>
          }
        </div>
        <ng-content select="[chart-header-actions]" />
      </div>
      <div class="p-5" [style.min-height.px]="minHeight">
        @if (loading) {
          <div class="flex items-center justify-center h-full">
            <div class="animate-spin rounded-full h-8 w-8 border-2 border-navy border-t-transparent"></div>
          </div>
        } @else {
          <ng-content />
        }
      </div>
    </div>
  `,
})
export class ChartCardComponent {
  @Input({ required: true }) title = '';
  @Input() subtitle = '';
  @Input() loading = false;
  @Input() minHeight = 280;
}
