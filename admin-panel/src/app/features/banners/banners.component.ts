import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

interface BannerDto { id: string; title: string; placement: string; isActive: boolean; displayOrder: number; linkUrl: string | null; }

@Component({
  selector: 'app-banners',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Banner Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">All Banners</h2>
        </div>
        @if (banners$ | async; as banners) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Title</th>
                  <th class="px-5 py-3 text-left">Placement</th>
                  <th class="px-5 py-3 text-center">Order</th>
                  <th class="px-5 py-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody>
                @for (b of banners; track b.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ b.title }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ b.placement }}</td>
                    <td class="px-5 py-3 text-center text-muted">{{ b.displayOrder }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="b.isActive ? 'active' : 'suspended'" /></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading banners…</div>
        }
      </div>
    </div>
  `,
})
export class BannersComponent implements OnInit {
  banners$: Observable<BannerDto[]> | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.banners$ = this.http.get<BannerDto[]>('/api/v1/admin/banners').pipe(catchError(() => of([])));
  }
}
