import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  UserService,
  WalletBalance,
  WalletTransaction,
} from '../../core/services/user.service';
import { SkeletonLoaderComponent } from '../../shared/components/skeleton-loader.component';

const QUICK_AMOUNTS = [500, 1000, 2000, 5000];

@Component({
  selector: 'app-wallet',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, NgClass, ReactiveFormsModule, RouterLink, SkeletonLoaderComponent],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6">
      <a routerLink="/account" class="inline-flex items-center gap-1 text-sm text-muted hover:text-navy mb-6">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
        Back to Account
      </a>

      <h1 class="font-display text-2xl font-bold text-dark mb-6">My Wallet</h1>

      <!-- Balance Card -->
      @if (loadingBalance()) {
        <app-skeleton-loader height="120px" cssClass="rounded-2xl mb-6" />
      } @else {
        <div class="rounded-2xl bg-gradient-to-br from-navy to-navy/80 text-white p-6 mb-6 shadow-lg">
          <p class="text-sm opacity-70 mb-1">Available Balance</p>
          <p class="text-4xl font-display font-bold tracking-tight">
            {{ wallet()?.balance | currency:'INR':'symbol-narrow':'1.0-0' }}
          </p>
          <p class="text-xs opacity-60 mt-1">StyleNest Cash</p>
        </div>
      }

      <!-- Add Money -->
      <div class="bg-card rounded-2xl border border-border p-5 mb-6">
        <h2 class="text-sm font-semibold text-muted uppercase tracking-widest mb-4">Add Money</h2>

        <!-- Quick amounts -->
        <div class="flex flex-wrap gap-2 mb-4">
          @for (amt of quickAmounts; track amt) {
            <button type="button" (click)="setAmount(amt)"
                    class="px-4 py-2 rounded-xl border text-sm font-medium transition-colors"
                    [ngClass]="selectedAmount() === amt
                      ? 'bg-red text-white border-red'
                      : 'bg-bg border-border text-dark hover:border-red hover:text-red'">
              ₹{{ amt | number }}
            </button>
          }
        </div>

        <form [formGroup]="addMoneyForm" (ngSubmit)="addMoney()">
          <div class="relative mb-3">
            <span class="absolute left-4 top-1/2 -translate-y-1/2 text-muted font-medium">₹</span>
            <input type="number" formControlName="amount" min="1"
                   class="w-full pl-8 pr-4 py-3 rounded-xl border border-border bg-bg text-dark focus:outline-none focus:ring-2 focus:ring-red/30 focus:border-red transition-colors"
                   placeholder="Enter custom amount" />
          </div>
          @if (addError()) {
            <p class="text-xs text-red mb-2">{{ addError() }}</p>
          }
          @if (addSuccess()) {
            <p class="text-xs text-success mb-2">Money added successfully!</p>
          }
          <button type="submit" [disabled]="addMoneyForm.invalid || addingMoney()"
                  class="w-full h-11 rounded-xl bg-red text-white font-semibold text-sm hover:bg-red/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
            @if (addingMoney()) { Processing... } @else { Add Money }
          </button>
        </form>
      </div>

      <!-- Transaction History -->
      <div class="bg-card rounded-2xl border border-border p-5">
        <h2 class="text-sm font-semibold text-muted uppercase tracking-widest mb-4">Transaction History</h2>

        @if (loadingTxns()) {
          @for (i of [1,2,3]; track i) {
            <app-skeleton-loader height="52px" cssClass="rounded-xl mb-2" />
          }
        } @else if (transactions().length === 0) {
          <p class="text-sm text-muted text-center py-8">No transactions yet</p>
        } @else {
          <div class="divide-y divide-border">
            @for (txn of transactions(); track txn.id) {
              <div class="py-3 flex items-center justify-between">
                <div>
                  <p class="text-sm font-medium text-dark">{{ txn.description }}</p>
                  <p class="text-xs text-muted mt-0.5">{{ txn.createdAt | date:'mediumDate' }}</p>
                </div>
                <span class="text-sm font-semibold"
                      [ngClass]="txn.type === 'Credit' ? 'text-success' : 'text-red'">
                  {{ txn.type === 'Credit' ? '+' : '-' }}{{ txn.amount | currency:'INR':'symbol-narrow':'1.0-0' }}
                </span>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class WalletComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly fb = inject(FormBuilder);

  readonly quickAmounts = QUICK_AMOUNTS;
  readonly wallet = signal<WalletBalance | null>(null);
  readonly transactions = signal<WalletTransaction[]>([]);
  readonly loadingBalance = signal(true);
  readonly loadingTxns = signal(true);
  readonly addingMoney = signal(false);
  readonly addError = signal<string | null>(null);
  readonly addSuccess = signal(false);
  readonly selectedAmount = signal<number | null>(null);

  readonly addMoneyForm = this.fb.group({
    amount: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.loadBalance();
    this.loadTransactions();
  }

  setAmount(amt: number): void {
    this.selectedAmount.set(amt);
    this.addMoneyForm.patchValue({ amount: amt });
  }

  addMoney(): void {
    if (this.addMoneyForm.invalid) return;
    this.addingMoney.set(true);
    this.addError.set(null);
    this.addSuccess.set(false);

    this.userService.addMoneyToWallet(this.addMoneyForm.value.amount!).subscribe({
      next: (w) => {
        this.wallet.set(w);
        this.addingMoney.set(false);
        this.addSuccess.set(true);
        this.addMoneyForm.reset();
        this.selectedAmount.set(null);
        this.loadTransactions();
      },
      error: () => {
        this.addingMoney.set(false);
        this.addError.set('Failed to add money. Please try again.');
      },
    });
  }

  private loadBalance(): void {
    this.userService.getWallet().subscribe({
      next: (w) => { this.wallet.set(w); this.loadingBalance.set(false); },
      error: () => { this.loadingBalance.set(false); },
    });
  }

  private loadTransactions(): void {
    this.userService.getWalletTransactions().subscribe({
      next: (t) => { this.transactions.set(t); this.loadingTxns.set(false); },
      error: () => { this.loadingTxns.set(false); },
    });
  }
}
