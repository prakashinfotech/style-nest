import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'red' | 'navy' | 'gold' | 'green' | 'muted';

@Component({
  selector: 'app-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <span [ngClass]="variantClasses" class="inline-block px-2 py-0.5 text-xs font-semibold rounded">
      <ng-content />
    </span>
  `,
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'red';

  get variantClasses(): string {
    const map: Record<BadgeVariant, string> = {
      red:   'bg-red text-white',
      navy:  'bg-navy text-white',
      gold:  'bg-gold text-dark',
      green: 'bg-success text-white',
      muted: 'bg-gray-200 text-muted',
    };
    return map[this.variant];
  }
}
