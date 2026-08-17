import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AsyncPipe, CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Observable, switchMap } from 'rxjs';
import { OrderService, Order } from '../../core/services/order.service';
import { SkeletonLoaderComponent } from '../../shared/components/skeleton-loader.component';

const ORDER_STEPS = ['Placed', 'Confirmed', 'Shipped', 'Delivered'] as const;
type OrderStep = (typeof ORDER_STEPS)[number];

const CANCELLABLE_STATUSES = ['Placed', 'Confirmed'];
const RETURNABLE_STATUSES  = ['Delivered'];

const RETURN_REASONS = [
  'Wrong size / colour',
  'Defective / damaged product',
  'Not as described',
  'Changed my mind',
  'Better price available',
  'Other',
];

@Component({
  selector: 'app-order-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, AsyncPipe, RouterLink, CurrencyPipe, DatePipe, FormsModule, SkeletonLoaderComponent],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6">
      <a routerLink="/account" class="inline-flex items-center gap-1 text-sm text-muted hover:text-navy mb-6">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
        Back to Account
      </a>

      @if (order$ | async; as order) {
        <div class="bg-card rounded-xl border border-border p-5 mb-6">
          <div class="flex items-start justify-between flex-wrap gap-2 mb-4">
            <div>
              <h1 class="text-lg font-display font-bold text-dark">Order #{{ order.orderNumber }}</h1>
              <p class="text-xs text-muted mt-0.5">Placed on {{ order.createdAt | date:'mediumDate' }}</p>
            </div>
            <div class="flex items-center gap-2 flex-wrap">
              @if (isCancelled(order.status)) {
                <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-red/10 text-red">
                  Cancelled
                </span>
              }

              <!-- Cancel button for cancellable orders -->
              @if (canCancel(order.status)) {
                <button type="button" (click)="confirmCancel(order)"
                        [disabled]="cancelling()"
                        class="px-3 py-1 rounded-full text-xs font-semibold border border-red text-red hover:bg-red/10 disabled:opacity-50 transition-colors">
                  @if (cancelling()) { Cancelling... } @else { Cancel Order }
                </button>
              }

              <!-- Return button for delivered orders -->
              @if (canReturn(order.status)) {
                <button type="button" (click)="showReturnModal.set(true)"
                        class="px-3 py-1 rounded-full text-xs font-semibold border border-navy text-navy hover:bg-navy/10 transition-colors">
                  Request Return
                </button>
              }
            </div>
          </div>

          @if (cancelSuccess()) {
            <div class="bg-success/10 border border-success/30 rounded-xl p-3 mb-4">
              <p class="text-sm text-success font-medium">Order cancelled successfully.</p>
            </div>
          }
          @if (cancelError()) {
            <div class="bg-red/10 border border-red/30 rounded-xl p-3 mb-4">
              <p class="text-sm text-red">{{ cancelError() }}</p>
            </div>
          }

          <!-- Status Stepper -->
          @if (!isCancelled(order.status)) {
            <div class="mt-4 mb-2">
              <div class="flex items-center justify-between relative">
                <div class="absolute left-0 right-0 top-4 h-0.5 bg-border z-0 mx-8"></div>

                @for (step of steps; track step; let i = $index) {
                  <div class="flex flex-col items-center z-10 flex-1">
                    <div class="w-8 h-8 rounded-full flex items-center justify-center border-2 transition-colors"
                         [class]="getStepClass(step, order.status)">
                      @if (isCompleted(step, order.status)) {
                        <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
                        </svg>
                      } @else {
                        <span class="text-xs font-bold">{{ i + 1 }}</span>
                      }
                    </div>
                    <p class="text-xs mt-1 text-center font-medium"
                       [class]="isActiveOrCompleted(step, order.status) ? 'text-dark' : 'text-muted'">
                      {{ step }}
                    </p>
                  </div>
                }
              </div>
            </div>
          }
        </div>

        <!-- Order Items -->
        <div class="bg-card rounded-xl border border-border p-5 mb-4">
          <h2 class="text-sm font-semibold text-muted uppercase tracking-widest mb-4">Items</h2>
          <div class="divide-y divide-border">
            @for (item of order.items; track item.productId) {
              <div class="py-3 flex gap-4">
                @if (item.imageUrl) {
                  <img [src]="item.imageUrl" [alt]="item.name"
                       class="w-16 h-16 object-cover rounded-lg border border-border shrink-0" />
                }
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-dark line-clamp-2">{{ item.name }}</p>
                  <p class="text-xs text-muted mt-1">Qty: {{ item.quantity }}</p>
                </div>
                <p class="text-sm font-semibold text-dark shrink-0">
                  {{ item.price | currency:'INR':'symbol-narrow':'1.0-0' }}
                </p>
              </div>
            }
          </div>
        </div>

        <!-- Order Total -->
        <div class="bg-card rounded-xl border border-border p-5">
          <div class="flex justify-between text-sm">
            <span class="text-muted">Order Total</span>
            <span class="font-bold text-dark">{{ order.total | currency:'INR':'symbol-narrow':'1.0-0' }}</span>
          </div>
        </div>

        <!-- Return Request Modal -->
        @if (showReturnModal()) {
          <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
            <div class="bg-card rounded-2xl border border-border shadow-xl w-full max-w-md">
              <div class="flex items-center justify-between p-5 border-b border-border">
                <h2 class="font-display text-lg font-bold text-dark">Request Return</h2>
                <button type="button" (click)="showReturnModal.set(false)"
                        class="text-muted hover:text-dark transition-colors">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                  </svg>
                </button>
              </div>

              <div class="p-5">
                <p class="text-sm text-muted mb-4">Select the reason for return:</p>
                <div class="space-y-2 mb-5">
                  @for (reason of returnReasons; track reason) {
                    <label class="flex items-center gap-3 cursor-pointer">
                      <input type="radio" name="returnReason" [value]="reason"
                             [(ngModel)]="selectedReturnReason"
                             class="accent-red" />
                      <span class="text-sm text-dark">{{ reason }}</span>
                    </label>
                  }
                </div>

                @if (returnSuccess()) {
                  <div class="bg-success/10 border border-success/30 rounded-xl p-3 mb-4">
                    <p class="text-sm text-success font-medium">Return request submitted! We'll contact you within 24–48 hours.</p>
                  </div>
                }
                @if (returnError()) {
                  <p class="text-xs text-red mb-3">{{ returnError() }}</p>
                }

                <div class="flex gap-3">
                  <button type="button" (click)="showReturnModal.set(false)"
                          class="flex-1 h-11 rounded-xl border border-border text-dark text-sm font-medium hover:bg-bg transition-colors">
                    Cancel
                  </button>
                  <button type="button" (click)="submitReturn(order)"
                          [disabled]="!selectedReturnReason || returningOrder()"
                          class="flex-1 h-11 rounded-xl bg-navy text-white text-sm font-semibold disabled:opacity-50 hover:bg-navy/90 transition-colors">
                    @if (returningOrder()) { Submitting... } @else { Submit Request }
                  </button>
                </div>
              </div>
            </div>
          </div>
        }
      } @else {
        <app-skeleton-loader height="200px" cssClass="rounded-xl mb-4" />
        <app-skeleton-loader height="120px" cssClass="rounded-xl" />
      }
    </div>
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly route        = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);

  readonly steps: OrderStep[]  = [...ORDER_STEPS];
  readonly returnReasons        = RETURN_REASONS;

  order$!: Observable<Order>;

  readonly cancelling          = signal(false);
  readonly cancelSuccess       = signal(false);
  readonly cancelError         = signal<string | null>(null);
  readonly showReturnModal     = signal(false);
  readonly returningOrder      = signal(false);
  readonly returnSuccess       = signal(false);
  readonly returnError         = signal<string | null>(null);
  selectedReturnReason: string = '';

  ngOnInit(): void {
    this.order$ = this.route.paramMap.pipe(
      switchMap((params) => this.orderService.getOrder(params.get('id')!)),
    );
  }

  isCancelled(status: string): boolean {
    return status === 'Cancelled';
  }

  canCancel(status: string): boolean {
    return CANCELLABLE_STATUSES.includes(status) && !this.cancelSuccess();
  }

  canReturn(status: string): boolean {
    return RETURNABLE_STATUSES.includes(status) && !this.returnSuccess();
  }

  confirmCancel(order: Order): void {
    if (!confirm(`Cancel order #${order.orderNumber}?`)) return;
    this.cancelling.set(true);
    this.cancelError.set(null);
    this.orderService.cancelOrder(order.id).subscribe({
      next: () => { this.cancelling.set(false); this.cancelSuccess.set(true); },
      error: () => { this.cancelling.set(false); this.cancelError.set('Could not cancel the order. Please try again.'); },
    });
  }

  submitReturn(order: Order): void {
    if (!this.selectedReturnReason) return;
    this.returningOrder.set(true);
    this.returnError.set(null);
    const itemIds = order.items.map((i) => i.productId);
    this.orderService.requestReturn(order.id, this.selectedReturnReason, itemIds).subscribe({
      next: () => {
        this.returningOrder.set(false);
        this.returnSuccess.set(true);
      },
      error: () => {
        this.returningOrder.set(false);
        this.returnError.set('Could not submit return request. Please try again.');
      },
    });
  }

  private stepIndex(step: string): number {
    return ORDER_STEPS.indexOf(step as OrderStep);
  }

  isCompleted(step: OrderStep, status: string): boolean {
    return this.stepIndex(status) > this.stepIndex(step);
  }

  isActiveOrCompleted(step: OrderStep, status: string): boolean {
    return this.stepIndex(status) >= this.stepIndex(step);
  }

  getStepClass(step: OrderStep, status: string): string {
    const activeIdx = this.stepIndex(status);
    const stepIdx   = this.stepIndex(step);
    if (activeIdx > stepIdx)  return 'bg-red border-red';
    if (activeIdx === stepIdx) return 'bg-red border-red text-white';
    return 'bg-card border-border text-mid-gray';
  }
}
