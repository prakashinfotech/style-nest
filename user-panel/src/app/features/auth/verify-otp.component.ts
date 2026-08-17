import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md">
        <div class="bg-card rounded-2xl border border-border shadow-sm p-8">
          <div class="text-center mb-8">
            <div class="w-14 h-14 bg-navy/10 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg class="w-7 h-7 text-navy" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
              </svg>
            </div>
            <h1 class="font-display text-2xl font-bold text-dark">Enter OTP</h1>
            <p class="text-muted text-sm mt-2">
              We sent a code to <span class="font-medium text-dark">{{ email() }}</span>
            </p>
          </div>

          @if (error()) {
            <div class="bg-red/10 border border-red/30 rounded-xl p-3 mb-4">
              <p class="text-sm text-red">{{ error() }}</p>
            </div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-5">
              <label class="block text-sm font-medium text-dark mb-1.5" for="otp">One-Time Password</label>
              <input id="otp" type="text" formControlName="otp" maxlength="6" inputmode="numeric"
                     class="w-full px-4 py-3 rounded-xl border border-border bg-bg text-dark text-center text-xl tracking-widest placeholder-muted focus:outline-none focus:ring-2 focus:ring-red/30 focus:border-red transition-colors"
                     placeholder="000000" />
              @if (form.get('otp')?.touched && form.get('otp')?.errors?.['required']) {
                <p class="text-xs text-red mt-1">OTP is required</p>
              }
              @if (form.get('otp')?.touched && form.get('otp')?.errors?.['minlength']) {
                <p class="text-xs text-red mt-1">OTP must be 6 digits</p>
              }
            </div>

            <button type="submit" [disabled]="loading() || form.invalid"
                    class="w-full h-12 rounded-xl bg-red text-white font-semibold text-sm hover:bg-red/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
              @if (loading()) { Verifying... } @else { Verify OTP }
            </button>
          </form>

          <p class="text-center text-sm text-muted mt-6">
            Didn't receive the code?
            <a routerLink="/auth/forgot-password" class="text-red font-medium hover:underline ml-1">Resend</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class VerifyOtpComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly email = signal(this.route.snapshot.queryParamMap.get('email') ?? '');

  readonly form = this.fb.group({
    otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid || !this.email()) return;
    this.loading.set(true);
    this.error.set(null);

    this.authService.verifyOtp(this.email(), this.form.value.otp!).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/auth/reset-password'], {
          queryParams: { email: this.email(), otp: this.form.value.otp },
        });
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid or expired OTP. Please try again.');
      },
    });
  }
}
