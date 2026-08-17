import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white rounded-xl p-5 shadow-sm border border-border">
      <div class="flex items-start justify-between">
        <div>
          <p class="text-sm text-muted font-medium">{{ label }}</p>
          <p class="text-2xl font-bold text-dark mt-1">{{ value }}</p>
          @if (subtitle) {
            <p class="text-xs text-muted mt-1">{{ subtitle }}</p>
          }
        </div>
        <div class="w-10 h-10 rounded-lg flex items-center justify-center text-xl" [class]="iconBg">
          {{ icon }}
        </div>
      </div>
    </div>
  `,
})
export class KpiCardComponent {
  @Input({ required: true }) label   = '';
  @Input({ required: true }) value   = '';
  @Input({ required: true }) icon    = '';
  @Input() subtitle = '';
  @Input() iconBg   = 'bg-navy/10';
}
