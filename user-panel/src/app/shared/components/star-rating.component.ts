import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div class="flex items-center gap-0.5" [attr.aria-label]="rating + ' out of 5 stars'">
      @for (star of stars; track star) {
        <svg
          class="w-4 h-4"
          [class.text-gold]="star <= fullStars"
          [class.text-gray-300]="star > fullStars"
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
        </svg>
      }
      @if (showCount && reviewCount != null) {
        <span class="text-xs text-muted ml-1">({{ reviewCount }})</span>
      }
    </div>
  `,
})
export class StarRatingComponent {
  @Input({ required: true }) rating!: number;
  @Input() reviewCount: number | null = null;
  @Input() showCount = true;

  readonly stars = [1, 2, 3, 4, 5];

  get fullStars(): number {
    return Math.round(this.rating);
  }
}
