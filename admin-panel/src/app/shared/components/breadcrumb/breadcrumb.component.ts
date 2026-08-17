import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

interface Crumb {
  label: string;
  path: string | null;
}

const SEGMENT_LABELS: Record<string, string> = {
  dashboard: 'Dashboard',
  products: 'Products',
  orders: 'Orders',
  users: 'Users',
  categories: 'Categories',
  brands: 'Brands',
  banners: 'Banners',
  coupons: 'Coupons',
  super: 'Super Admin',
  sellers: 'Sellers',
  admins: 'Admins',
  seller: 'Seller',
  inventory: 'Inventory',
  analytics: 'Analytics',
  'review-moderation': 'Review Moderation',
  'platform-settings': 'Platform Settings',
  'audit-logs': 'Audit Logs',
  rbac: 'RBAC',
  create: 'Create',
  edit: 'Edit',
  payouts: 'Payouts',
};

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <nav aria-label="Breadcrumb" class="flex items-center gap-1 text-sm mb-4">
      <a routerLink="/" class="text-muted hover:text-navy transition-colors">Home</a>
      @for (crumb of crumbs(); track crumb.label; let last = $last) {
        <span class="text-mid-gray mx-1" aria-hidden="true">›</span>
        @if (!last && crumb.path) {
          <a [routerLink]="crumb.path" class="text-muted hover:text-navy transition-colors">
            {{ crumb.label }}
          </a>
        } @else {
          <span class="text-dark font-medium" aria-current="page">{{ crumb.label }}</span>
        }
      }
    </nav>
  `,
})
export class BreadcrumbComponent {
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);

  crumbs = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.buildCrumbs()),
    ),
    { initialValue: [] as Crumb[] },
  );

  private buildCrumbs(): Crumb[] {
    const url = this.router.url.split('?')[0];
    const segments = url.split('/').filter(Boolean);

    const crumbs: Crumb[] = [];
    let pathAccumulator = '';

    for (let i = 0; i < segments.length; i++) {
      const seg = segments[i];
      const label = SEGMENT_LABELS[seg] ?? this.titleCase(seg);
      pathAccumulator += '/' + seg;
      crumbs.push({ label, path: i < segments.length - 1 ? pathAccumulator : null });
    }

    return crumbs;
  }

  private titleCase(s: string): string {
    return s.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
