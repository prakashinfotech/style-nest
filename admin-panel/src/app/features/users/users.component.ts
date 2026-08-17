import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { AdminApiService, AdminUser, PagedResult } from '../../core/services/admin-api.service';

@Component({
  selector: 'app-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Customer Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">Registered Customers</h2>
        </div>

        @if (users$ | async; as page) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Name</th>
                  <th class="px-5 py-3 text-left">Email</th>
                  <th class="px-5 py-3 text-center">Verified</th>
                  <th class="px-5 py-3 text-left">Joined</th>
                </tr>
              </thead>
              <tbody>
                @for (u of page.items; track u.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ u.firstName }} {{ u.lastName }}</td>
                    <td class="px-5 py-3 text-muted">{{ u.email }}</td>
                    <td class="px-5 py-3 text-center">
                      <span [class]="u.emailConfirmed ? 'text-success' : 'text-error'">
                        {{ u.emailConfirmed ? '✓' : '✗' }}
                      </span>
                    </td>
                    <td class="px-5 py-3 text-muted text-xs">{{ u.createdAt | date:'mediumDate' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-border text-xs text-muted">
            {{ page.totalCount }} customers total
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading customers…</div>
        }
      </div>
    </div>
  `,
})
export class UsersComponent implements OnInit {
  users$: Observable<PagedResult<AdminUser>> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.users$ = this.api.getUsers().pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 20 })));
  }
}
