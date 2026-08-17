import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass],
  template: `
    <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" [ngClass]="classes">
      {{ status }}
    </span>
  `,
})
export class StatusBadgeComponent {
  @Input({ required: true }) status = '';

  get classes(): string {
    switch (this.status?.toLowerCase()) {
      case 'active':
      case 'delivered':
      case 'approved':
      case 'completed':
        return 'bg-success/10 text-success';
      case 'pending':
      case 'processing':
        return 'bg-warning/10 text-warning';
      case 'rejected':
      case 'suspended':
      case 'cancelled':
      case 'failed':
        return 'bg-error/10 text-error';
      case 'shipped':
        return 'bg-blue/10 text-blue';
      default:
        return 'bg-mid-gray/20 text-muted';
    }
  }
}
