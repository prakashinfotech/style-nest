import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { login, verifyMfa, resetMfaStep } from '../../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError, selectMfaStep, selectMfaToken } from '../../../store/auth/auth.selectors';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, AsyncPipe],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center p-4">
      <div class="w-full max-w-md bg-white rounded-xl shadow-md p-8">

        <!-- Logo -->
        <div class="flex items-center gap-3 mb-8">
          <div class="w-10 h-10 bg-navy rounded-xl flex items-center justify-center">
            <span class="text-white font-display font-bold text-lg">T</span>
          </div>
          <div>
            <h1 class="font-display font-bold text-navy text-xl leading-tight">StyleNest</h1>
            <p class="text-xs text-muted">Administration Portal</p>
          </div>
        </div>

        @if (mfaStep() === 'mfa') {
          <!-- ── Step 2: MFA verification ── -->
          <div class="mb-4 p-3 bg-blue-50 text-blue-800 text-sm rounded-lg">
            A 6-digit verification code was sent to your email.
            In development, check the Auth API container logs for the OTP.
          </div>

          <h2 class="text-lg font-semibold text-dark mb-6">Enter verification code</h2>

          @if (error$ | async; as error) {
            <div class="mb-4 p-3 bg-red/10 text-red text-sm rounded-lg">{{ error }}</div>
          }

          <form [formGroup]="mfaForm" (ngSubmit)="submitMfa()" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-dark mb-1">6-digit OTP code</label>
              <input
                formControlName="otpCode"
                type="text"
                inputmode="numeric"
                maxlength="6"
                autocomplete="one-time-code"
                class="w-full px-3 py-2 border border-border rounded-lg text-sm text-center tracking-widest text-lg focus:outline-none focus:ring-2 focus:ring-navy/30 focus:border-navy transition-colors"
                placeholder="000000">
              @if (mfaForm.get('otpCode')?.invalid && mfaForm.get('otpCode')?.touched) {
                <p class="text-red text-xs mt-1">Enter the 6-digit code</p>
              }
            </div>

            <button
              type="submit"
              [disabled]="(loading$ | async) || mfaForm.invalid"
              class="w-full py-2.5 bg-navy text-white font-semibold rounded-lg text-sm hover:bg-navy/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
              @if (loading$ | async) { Verifying… } @else { Verify & Sign in }
            </button>
          </form>

          <p class="text-xs text-muted text-center mt-4">
            Didn't receive the code?
            <button type="button" (click)="backToLogin()" class="text-navy underline ml-1">Go back</button>
          </p>

        } @else {
          <!-- ── Step 1: Email / password ── -->
          <h2 class="text-lg font-semibold text-dark mb-6">Sign in to your account</h2>

          @if (error$ | async; as error) {
            <div class="mb-4 p-3 bg-red/10 text-red text-sm rounded-lg">{{ error }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-dark mb-1">Email address</label>
              <input
                formControlName="email"
                type="email"
                autocomplete="email"
                class="w-full px-3 py-2 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-navy/30 focus:border-navy transition-colors"
                placeholder="admin@mailinator.com">
              @if (form.get('email')?.invalid && form.get('email')?.touched) {
                <p class="text-red text-xs mt-1">Valid email is required</p>
              }
            </div>

            <div>
              <label class="block text-sm font-medium text-dark mb-1">Password</label>
              <input
                formControlName="password"
                type="password"
                autocomplete="current-password"
                class="w-full px-3 py-2 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-navy/30 focus:border-navy transition-colors"
                placeholder="••••••••">
              @if (form.get('password')?.invalid && form.get('password')?.touched) {
                <p class="text-red text-xs mt-1">Password is required</p>
              }
            </div>

            <button
              type="submit"
              [disabled]="(loading$ | async) || form.invalid"
              class="w-full py-2.5 bg-navy text-white font-semibold rounded-lg text-sm hover:bg-navy/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
              @if (loading$ | async) { Signing in… } @else { Sign in }
            </button>
          </form>

          <p class="text-xs text-muted text-center mt-6">
            Default accounts: <span class="font-mono">superadmin@mailinator.com</span> / <span class="font-mono">Test&#64;123</span>
          </p>
        }

      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly loading$  = this.store.select(selectAuthLoading);
  readonly error$    = this.store.select(selectAuthError);
  readonly mfaStep   = toSignal(this.store.select(selectMfaStep), { initialValue: 'login' as const });
  readonly mfaToken  = toSignal(this.store.select(selectMfaToken), { initialValue: null });

  readonly form = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  readonly mfaForm = this.fb.nonNullable.group({
    otpCode: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    const { email, password } = this.form.getRawValue();
    this.store.dispatch(login({ email, password }));
  }

  submitMfa(): void {
    if (this.mfaForm.invalid) return;
    const token = this.mfaToken();
    if (!token) return;
    const { otpCode } = this.mfaForm.getRawValue();
    this.store.dispatch(verifyMfa({ mfaToken: token, otpCode }));
  }

  backToLogin(): void {
    this.mfaForm.reset();
    this.store.dispatch(resetMfaStep());
  }
}
