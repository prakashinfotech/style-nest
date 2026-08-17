import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div
      [class]="'animate-pulse bg-gray-200 rounded ' + cssClass"
      [style.height]="height"
      [style.width]="width"
    ></div>
  `,
})
export class SkeletonLoaderComponent {
  @Input() height = '1rem';
  @Input() width  = '100%';
  @Input() cssClass = '';
}
