import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password === confirm ? null : { mismatch: true };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md">
        <div class="bg-card rounded-2xl border border-border shadow-sm p-8">
          <div class="text-center mb-8">
            <h1 class="font-display text-2xl font-bold text-dark">Reset Password</h1>
            <p class="text-muted text-sm mt-2">Create a new password for your account</p>
          </div>

          @if (success()) {
            <div class="bg-success/10 border border-success/30 rounded-xl p-4 mb-6 text-center">
              <svg class="w-8 h-8 text-success mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              <p class="text-sm font-medium text-success">Password reset successfully!</p>
              <a routerLink="/auth/login" class="mt-3 inline-block text-sm text-blue underline">Sign in now →</a>
            </div>
          }

          @if (error()) {
            <div class="bg-red/10 border border-red/30 rounded-xl p-3 mb-4">
              <p class="text-sm text-red">{{ error() }}</p>
            </div>
          }

          @if (!success()) {
            <form [formGroup]="form" (ngSubmit)="submit()">
              <div class="mb-4">
                <label class="block text-sm font-medium text-dark mb-1.5" for="newPassword">New password</label>
                <input id="newPassword" type="password" formControlName="newPassword"
                       class="w-full px-4 py-3 rounded-xl border border-border bg-bg text-dark placeholder-muted focus:outline-none focus:ring-2 focus:ring-red/30 focus:border-red transition-colors"
                       placeholder="At least 8 characters" />
                @if (form.get('newPassword')?.touched && form.get('newPassword')?.errors?.['required']) {
                  <p class="text-xs text-red mt-1">Password is required</p>
                }
                @if (form.get('newPassword')?.touched && form.get('newPassword')?.errors?.['minlength']) {
                  <p class="text-xs text-red mt-1">Password must be at least 8 characters</p>
                }
              </div>

              <div class="mb-6">
                <label class="block text-sm font-medium text-dark mb-1.5" for="confirmPassword">Confirm password</label>
                <input id="confirmPassword" type="password" formControlName="confirmPassword"
                       class="w-full px-4 py-3 rounded-xl border border-border bg-bg text-dark placeholder-muted focus:outline-none focus:ring-2 focus:ring-red/30 focus:border-red transition-colors"
                       placeholder="Repeat your new password" />
                @if (form.errors?.['mismatch'] && form.get('confirmPassword')?.touched) {
                  <p class="text-xs text-red mt-1">Passwords do not match</p>
                }
              </div>

              <button type="submit" [disabled]="loading() || form.invalid"
                      class="w-full h-12 rounded-xl bg-red text-white font-semibold text-sm hover:bg-red/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
                @if (loading()) { Resetting... } @else { Reset Password }
              </button>
            </form>
          }
        </div>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  private readonly otp = this.route.snapshot.queryParamMap.get('otp') ?? '';

  readonly form = this.fb.group(
    {
      newPassword:     ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  readonly loading = signal(false);
  readonly success = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.authService.resetPassword(this.email, this.otp, this.form.value.newPassword!).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not reset password. The OTP may have expired.');
      },
    });
  }
}
