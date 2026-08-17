import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  link?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- DESIGN.md §4.17 Breadcrumb — nav landmark, aria-current on last item -->
    <nav aria-label="Breadcrumb" class="mb-4">
      <ol class="flex items-center flex-wrap gap-x-1 gap-y-1 text-[13px] font-sans">
        @for (crumb of crumbs; track crumb.label; let last = $last) {
          <li class="flex items-center gap-x-1">
            @if (!last && crumb.link) {
              <a
                [routerLink]="crumb.link"
                class="text-muted hover:text-red transition-colors underline-offset-2 hover:underline"
              >
                {{ crumb.label }}
              </a>
              <!-- Separator › -->
              <span class="text-mid-gray select-none" aria-hidden="true">&rsaquo;</span>
            } @else {
              <span
                class="text-dark font-medium"
                [attr.aria-current]="last ? 'page' : null"
              >
                {{ crumb.label }}
              </span>
            }
          </li>
        }
      </ol>
    </nav>
  `,
})
export class BreadcrumbComponent {
  @Input({ required: true }) crumbs: BreadcrumbItem[] = [];
}
