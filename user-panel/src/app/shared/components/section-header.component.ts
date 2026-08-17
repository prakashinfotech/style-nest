import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-section-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- DESIGN.md §4.6 Section Header — eyebrow + title + red divider + View All link -->
    <div class="flex items-end justify-between mb-6">
      <div>
        @if (eyebrow) {
          <p class="text-[11px] font-medium uppercase tracking-widest text-red mb-1">
            {{ eyebrow }}
          </p>
        }
        <h2 class="font-display text-2xl md:text-[28px] font-semibold text-dark leading-tight">
          {{ title }}
        </h2>
        <!-- Red divider bar -->
        <div class="mt-2 h-0.5 w-10 bg-red rounded-full"></div>
      </div>

      @if (viewAllLink) {
        <a
          [routerLink]="viewAllLink"
          [queryParams]="viewAllParams ?? null"
          class="text-[13px] font-medium text-red hover:underline underline-offset-2 transition whitespace-nowrap ml-4 pb-1"
          [attr.aria-label]="'View all ' + title"
        >
          View All &rarr;
        </a>
      }
    </div>
  `,
})
export class SectionHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() eyebrow: string | null = null;
  @Input() viewAllLink: string | null = null;
  @Input() viewAllParams: Record<string, string> | null = null;
}
