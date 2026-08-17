import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
} from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { AdminApiService, AuditLog, PagedResult } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

interface FilterState {
  page: number;
  action: string;
  actorEmail: string;
}

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, FormsModule, StatusBadgeComponent],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-xl font-bold text-dark">Audit Logs</h1>
        <p class="text-sm text-muted mt-0.5">All admin actions logged for security and compliance.</p>
      </div>

      <!-- Filters -->
      <div class="bg-white rounded-xl shadow-sm border border-border p-4 flex flex-col sm:flex-row gap-3">
        <input
          type="text"
          placeholder="Filter by actor email"
          class="flex-1 border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
          [(ngModel)]="filterEmail"
        />
        <input
          type="text"
          placeholder="Filter by action (e.g. APPROVE_SELLER)"
          class="flex-1 border border-border rounded-lg px-3 py-2 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
          [(ngModel)]="filterAction"
        />
        <button
          type="button"
          class="px-4 py-2 bg-navy text-white text-sm rounded-lg hover:bg-navy/90 transition-colors whitespace-nowrap"
          (click)="applyFilter()"
        >Search</button>
        <button
          type="button"
          class="px-4 py-2 border border-border text-dark text-sm rounded-lg hover:bg-bg transition-colors whitespace-nowrap"
          (click)="clearFilter()"
        >Clear</button>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                <th class="px-5 py-3 text-left">Timestamp</th>
                <th class="px-5 py-3 text-left">Actor</th>
                <th class="px-5 py-3 text-left">Role</th>
                <th class="px-5 py-3 text-left">Action</th>
                <th class="px-5 py-3 text-left">Resource</th>
                <th class="px-5 py-3 text-left">IP Address</th>
              </tr>
            </thead>
            <tbody>
              @if (result$ | async; as vm) {
                @if (!vm.result.items.length) {
                  <tr>
                    <td colspan="6" class="px-5 py-12 text-center text-muted">No audit logs found.</td>
                  </tr>
                } @else {
                  @for (log of vm.result.items; track log.id) {
                    <tr class="border-b border-border/50 hover:bg-bg/40 transition-colors">
                      <td class="px-5 py-2.5 text-muted text-xs whitespace-nowrap">
                        {{ log.timestamp | date:'dd MMM yy, HH:mm' }}
                      </td>
                      <td class="px-5 py-2.5 text-dark font-medium text-xs">{{ log.actorEmail }}</td>
                      <td class="px-5 py-2.5">
                        <app-status-badge [status]="log.actorRole" />
                      </td>
                      <td class="px-5 py-2.5">
                        <span class="inline-block bg-navy/5 text-navy text-xs font-mono px-2 py-0.5 rounded">
                          {{ log.action }}
                        </span>
                      </td>
                      <td class="px-5 py-2.5 text-dark text-xs">
                        {{ log.resource }}{{ log.resourceId ? ' #' + log.resourceId.slice(0, 8) : '' }}
                      </td>
                      <td class="px-5 py-2.5 text-muted text-xs font-mono">{{ log.ipAddress }}</td>
                    </tr>
                  }
                }

                @if (vm.totalPages > 1) {
                  <tr>
                    <td colspan="6" class="px-5 py-3 border-t border-border">
                      <div class="flex items-center justify-between text-sm">
                        <span class="text-muted">Page {{ currentFilter().page }} of {{ vm.totalPages }}</span>
                        <div class="flex gap-2">
                          <button
                            type="button"
                            class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40 transition-colors"
                            [disabled]="currentFilter().page <= 1"
                            (click)="goToPage(currentFilter().page - 1)"
                          >Prev</button>
                          <button
                            type="button"
                            class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40 transition-colors"
                            [disabled]="currentFilter().page >= vm.totalPages"
                            (click)="goToPage(currentFilter().page + 1)"
                          >Next</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                }
              } @else {
                @for (i of skeleton; track i) {
                  <tr class="border-b border-border/50">
                    @for (j of skeleton6; track j) {
                      <td class="px-5 py-3"><div class="h-4 bg-bg rounded animate-pulse"></div></td>
                    }
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class AuditLogsComponent implements OnInit {
  filterEmail = '';
  filterAction = '';

  readonly skeleton = [1, 2, 3, 4, 5, 6, 7, 8];
  readonly skeleton6 = [1, 2, 3, 4, 5, 6];
  readonly pageSize = 20;

  private filter$ = new BehaviorSubject<FilterState>({ page: 1, action: '', actorEmail: '' });

  currentFilter = signal<FilterState>({ page: 1, action: '', actorEmail: '' });

  result$: Observable<{ result: PagedResult<AuditLog>; totalPages: number }> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.result$ = this.filter$.pipe(
      tap((f) => this.currentFilter.set(f)),
      switchMap((f) =>
        this.api
          .getAuditLogs(f.page, this.pageSize, f.action || undefined, f.actorEmail || undefined)
          .pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: this.pageSize }))),
      ),
      map((result) => ({
        result,
        totalPages: Math.ceil(result.totalCount / this.pageSize) || 1,
      })),
    );
  }

  applyFilter(): void {
    this.filter$.next({ page: 1, action: this.filterAction, actorEmail: this.filterEmail });
  }

  clearFilter(): void {
    this.filterEmail = '';
    this.filterAction = '';
    this.filter$.next({ page: 1, action: '', actorEmail: '' });
  }

  goToPage(page: number): void {
    const cur = this.filter$.value;
    this.filter$.next({ ...cur, page });
  }
}
