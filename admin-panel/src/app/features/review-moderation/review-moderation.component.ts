import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
} from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { AdminApiService, PagedResult, ReviewModerationItem } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-review-moderation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, StatusBadgeComponent, ConfirmDialogComponent],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-bold text-dark">Review Moderation</h1>
          <p class="text-sm text-muted mt-0.5">Approve or remove customer product reviews.</p>
        </div>

        <!-- Status filter tabs -->
        <div class="flex gap-1 bg-bg rounded-lg p-1">
          @for (tab of tabs; track tab.value) {
            <button
              type="button"
              class="px-3 py-1.5 text-xs rounded-md font-medium transition-colors"
              [class]="activeTab() === tab.value
                ? 'bg-white shadow-sm text-dark'
                : 'text-muted hover:text-dark'"
              (click)="setTab(tab.value)"
            >
              {{ tab.label }}
            </button>
          }
        </div>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                <th class="px-5 py-3 text-left">Product</th>
                <th class="px-5 py-3 text-left">Author</th>
                <th class="px-5 py-3 text-center w-20">Rating</th>
                <th class="px-5 py-3 text-left">Review</th>
                <th class="px-5 py-3 text-center w-24">Status</th>
                <th class="px-5 py-3 text-left w-32">Date</th>
                <th class="px-5 py-3 text-right w-36">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (result$ | async; as vm) {
                @if (!vm.result.items.length) {
                  <tr>
                    <td colspan="7" class="px-5 py-12 text-center text-muted">No reviews to moderate.</td>
                  </tr>
                } @else {
                  @for (review of vm.result.items; track review.id) {
                    <tr class="border-b border-border/50 hover:bg-bg/40 transition-colors align-top">
                      <td class="px-5 py-3 text-dark font-medium text-xs max-w-[140px]">
                        <span class="line-clamp-2">{{ review.productName }}</span>
                      </td>
                      <td class="px-5 py-3 text-muted text-xs">{{ review.authorEmail }}</td>
                      <td class="px-5 py-3 text-center">
                        <span class="text-gold font-bold">{{ review.rating }}</span>
                        <span class="text-mid-gray text-xs">/5</span>
                      </td>
                      <td class="px-5 py-3 max-w-[240px]">
                        <p class="text-dark font-medium text-xs">{{ review.title }}</p>
                        <p class="text-muted text-xs mt-0.5 line-clamp-2">{{ review.body }}</p>
                      </td>
                      <td class="px-5 py-3 text-center">
                        <app-status-badge [status]="review.status" />
                      </td>
                      <td class="px-5 py-3 text-muted text-xs whitespace-nowrap">
                        {{ review.createdAt | date:'dd MMM yy' }}
                      </td>
                      <td class="px-5 py-3 text-right space-x-1.5">
                        @if (review.status !== 'Approved') {
                          <button
                            type="button"
                            class="text-xs font-medium px-2.5 py-1 rounded border border-success/40 text-success hover:bg-success/10 transition-colors"
                            (click)="confirmApprove(review)"
                          >Approve</button>
                        }
                        <button
                          type="button"
                          class="text-xs font-medium px-2.5 py-1 rounded border border-red/40 text-red hover:bg-red/10 transition-colors"
                          (click)="confirmDelete(review)"
                        >Delete</button>
                      </td>
                    </tr>
                  }
                }

                @if (vm.totalPages > 1) {
                  <tr>
                    <td colspan="7" class="px-5 py-3 border-t border-border">
                      <div class="flex items-center justify-between text-sm">
                        <span class="text-muted">Page {{ currentPage() }} of {{ vm.totalPages }}</span>
                        <div class="flex gap-2">
                          <button type="button"
                            class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40"
                            [disabled]="currentPage() <= 1"
                            (click)="goToPage(currentPage() - 1)">Prev</button>
                          <button type="button"
                            class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40"
                            [disabled]="currentPage() >= vm.totalPages"
                            (click)="goToPage(currentPage() + 1)">Next</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                }
              } @else {
                @for (i of skeleton; track i) {
                  <tr class="border-b border-border/50">
                    @for (j of [1,2,3,4,5,6,7]; track j) {
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

    <!-- Confirm Approve -->
    <app-confirm-dialog
      [open]="!!pendingApprove()"
      title="Approve Review"
      message="This review will be visible to all users."
      confirmLabel="Approve"
      variant="default"
      (confirm)="doApprove()"
      (cancel)="pendingApprove.set(null)"
    />

    <!-- Confirm Delete -->
    <app-confirm-dialog
      [open]="!!pendingDelete()"
      title="Delete Review"
      message="This review will be permanently removed."
      confirmLabel="Delete"
      variant="danger"
      (confirm)="doDelete()"
      (cancel)="pendingDelete.set(null)"
    />
  `,
})
export class ReviewModerationComponent implements OnInit {
  readonly tabs = [
    { label: 'Pending',  value: 'Pending' },
    { label: 'Approved', value: 'Approved' },
    { label: 'All',      value: '' },
  ];

  readonly skeleton = [1, 2, 3, 4, 5, 6];
  readonly pageSize = 20;

  activeTab = signal('Pending');
  currentPage = signal(1);
  pendingApprove = signal<ReviewModerationItem | null>(null);
  pendingDelete = signal<ReviewModerationItem | null>(null);

  private filter$ = new BehaviorSubject<{ page: number; status: string }>({ page: 1, status: 'Pending' });

  result$: Observable<{ result: PagedResult<ReviewModerationItem>; totalPages: number }> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.result$ = this.filter$.pipe(
      tap((f) => this.currentPage.set(f.page)),
      switchMap((f) =>
        this.api
          .getReviewsForModeration(f.page, this.pageSize, f.status || undefined)
          .pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: this.pageSize }))),
      ),
      map((result) => ({
        result,
        totalPages: Math.ceil(result.totalCount / this.pageSize) || 1,
      })),
    );
  }

  setTab(status: string): void {
    this.activeTab.set(status);
    this.filter$.next({ page: 1, status });
  }

  goToPage(page: number): void {
    const cur = this.filter$.value;
    this.filter$.next({ ...cur, page });
  }

  confirmApprove(review: ReviewModerationItem): void {
    this.pendingApprove.set(review);
  }

  confirmDelete(review: ReviewModerationItem): void {
    this.pendingDelete.set(review);
  }

  doApprove(): void {
    const review = this.pendingApprove();
    if (!review) return;
    this.pendingApprove.set(null);
    this.api.approveReview(review.id).subscribe({
      complete: () => this.filter$.next({ ...this.filter$.value }),
    });
  }

  doDelete(): void {
    const review = this.pendingDelete();
    if (!review) return;
    this.pendingDelete.set(null);
    this.api.deleteReview(review.id).subscribe({
      complete: () => this.filter$.next({ ...this.filter$.value }),
    });
  }
}
