import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
} from '@angular/core';
import { AsyncPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { PagedResult, SellerApiService, SellerPayout } from '../../../core/services/seller-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-seller-payouts',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, CurrencyPipe, DatePipe, StatusBadgeComponent],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-xl font-bold text-dark">Payout History</h1>
        <p class="text-sm text-muted mt-0.5">View your earnings payouts processed by the platform.</p>
      </div>

      <!-- Summary cards -->
      @if (summary$ | async; as s) {
        <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
          <div class="bg-white rounded-xl p-5 shadow-sm border border-border">
            <p class="text-xs text-muted font-medium">Total Paid Out</p>
            <p class="text-2xl font-bold text-dark mt-1">{{ s.totalPaid | currency:'INR':'symbol-narrow':'1.0-0' }}</p>
          </div>
          <div class="bg-white rounded-xl p-5 shadow-sm border border-border">
            <p class="text-xs text-muted font-medium">Pending Amount</p>
            <p class="text-2xl font-bold text-gold mt-1">{{ s.totalPending | currency:'INR':'symbol-narrow':'1.0-0' }}</p>
          </div>
          <div class="bg-white rounded-xl p-5 shadow-sm border border-border col-span-2 md:col-span-1">
            <p class="text-xs text-muted font-medium">Payouts Count</p>
            <p class="text-2xl font-bold text-dark mt-1">{{ s.count }}</p>
          </div>
        </div>
      }

      <!-- Table -->
      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">Payout Records</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                <th class="px-5 py-3 text-left">Period</th>
                <th class="px-5 py-3 text-right">Amount</th>
                <th class="px-5 py-3 text-center">Status</th>
                <th class="px-5 py-3 text-left">Processed On</th>
                <th class="px-5 py-3 text-left">Transaction Ref</th>
              </tr>
            </thead>
            <tbody>
              @if (result$ | async; as vm) {
                @if (!vm.result.items.length) {
                  <tr>
                    <td colspan="5" class="px-5 py-12 text-center text-muted">No payouts yet.</td>
                  </tr>
                } @else {
                  @for (payout of vm.result.items; track payout.id) {
                    <tr class="border-b border-border/50 hover:bg-bg/40 transition-colors">
                      <td class="px-5 py-3 text-dark text-xs">
                        {{ payout.periodStart | date:'dd MMM yy' }} – {{ payout.periodEnd | date:'dd MMM yy' }}
                      </td>
                      <td class="px-5 py-3 text-right font-semibold text-dark">
                        {{ payout.amount | currency:'INR':'symbol-narrow':'1.0-0' }}
                      </td>
                      <td class="px-5 py-3 text-center">
                        <app-status-badge [status]="payout.status" />
                      </td>
                      <td class="px-5 py-3 text-muted text-xs">
                        {{ payout.processedAt ? (payout.processedAt | date:'dd MMM yy') : '—' }}
                      </td>
                      <td class="px-5 py-3 text-muted text-xs font-mono">
                        {{ payout.transactionRef ?? '—' }}
                      </td>
                    </tr>
                  }
                }

                @if (vm.totalPages > 1) {
                  <tr>
                    <td colspan="5" class="px-5 py-3 border-t border-border">
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
                    @for (j of [1,2,3,4,5]; track j) {
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
export class SellerPayoutsComponent implements OnInit {
  readonly skeleton = [1, 2, 3, 4, 5];
  readonly pageSize = 20;

  currentPage = signal(1);

  private page$ = new BehaviorSubject(1);

  result$: Observable<{ result: PagedResult<SellerPayout>; totalPages: number }> | null = null;
  summary$: Observable<{ totalPaid: number; totalPending: number; count: number }> | null = null;

  constructor(private api: SellerApiService) {}

  ngOnInit(): void {
    this.result$ = this.page$.pipe(
      tap((p) => this.currentPage.set(p)),
      switchMap((p) =>
        this.api.getPayouts(p, this.pageSize)
          .pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: this.pageSize }))),
      ),
      map((result) => ({
        result,
        totalPages: Math.ceil(result.totalCount / this.pageSize) || 1,
      })),
    );

    this.summary$ = this.api.getPayouts(1, 1000).pipe(
      map((r) => ({
        totalPaid:    r.items.filter((p) => p.status === 'Paid').reduce((s, p) => s + p.amount, 0),
        totalPending: r.items.filter((p) => p.status === 'Pending').reduce((s, p) => s + p.amount, 0),
        count: r.totalCount,
      })),
      catchError(() => of({ totalPaid: 0, totalPending: 0, count: 0 })),
    );
  }

  goToPage(page: number): void {
    this.page$.next(page);
  }
}
