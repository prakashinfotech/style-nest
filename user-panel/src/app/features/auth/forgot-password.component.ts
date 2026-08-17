import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md">
        <div class="bg-card rounded-2xl border border-border shadow-sm p-8">
          <div class="text-center mb-8">
            <h1 class="font-display text-2xl font-bold text-dark">Forgot Password</h1>
            <p class="text-muted text-sm mt-2">Enter your email to receive a one-time password</p>
          </div>

          @if (success()) {
            <div class="bg-success/10 border border-success/30 rounded-xl p-4 mb-6 text-center">
              <svg class="w-8 h-8 text-success mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              <p class="text-sm font-medium text-success">OTP sent! Check your email inbox.</p>
              <a [routerLink]="['/auth/verify-otp']" [queryParams]="{ email: form.value.email }"
                 class="mt-3 inline-block text-sm text-blue underline">Enter OTP →</a>
            </div>
          }

          @if (error()) {
            <div class="bg-red/10 border border-red/30 rounded-xl p-3 mb-4">
              <p class="text-sm text-red">{{ error() }}</p>
            </div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-5">
              <label class="block text-sm font-medium text-dark mb-1.5" for="email">Email address</label>
              <input id="email" type="email" formControlName="email"
                     class="w-full px-4 py-3 rounded-xl border border-border bg-bg text-dark placeholder-muted focus:outline-none focus:ring-2 focus:ring-red/30 focus:border-red transition-colors"
                     placeholder="you@example.com" />
              @if (form.get('email')?.touched && form.get('email')?.errors?.['required']) {
                <p class="text-xs text-red mt-1">Email is required</p>
              }
              @if (form.get('email')?.touched && form.get('email')?.errors?.['email']) {
                <p class="text-xs text-red mt-1">Enter a valid email address</p>
              }
            </div>

            <button type="submit" [disabled]="loading() || form.invalid"
                    class="w-full h-12 rounded-xl bg-red text-white font-semibold text-sm hover:bg-red/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
              @if (loading()) { Sending... } @else { Send OTP }
            </button>
          </form>

          <p class="text-center text-sm text-muted mt-6">
            Remember your password?
            <a routerLink="/auth/login" class="text-red font-medium hover:underline ml-1">Sign in</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly loading = signal(false);
  readonly success = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.authService.forgotPassword(this.form.value.email!).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not send OTP. Check the email and try again.');
      },
    });
  }
}
