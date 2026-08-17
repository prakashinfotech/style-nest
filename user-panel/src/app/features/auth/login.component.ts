import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError } from '../../store/auth/auth.selectors';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md bg-white rounded-2xl shadow-lg p-8">

        <!-- Header -->
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold text-navy">Welcome Back</h1>
          <p class="text-muted text-sm mt-1">Sign in to your StyleNest account</p>
        </div>

        <!-- Error banner -->
        @if (error$ | async; as error) {
          <div class="mb-4 px-4 py-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
            {{ error }}
          </div>
        }

        <!-- Form -->
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-5" novalidate>

          <div>
            <label class="block text-sm font-medium text-dark mb-1" for="email">Email</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              autocomplete="email"
              placeholder="you@example.com"
              class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
              [class.border-red]="isInvalid('email')"
            />
            @if (isInvalid('email')) {
              <p class="mt-1 text-xs text-red">Enter a valid email address.</p>
            }
          </div>

          <div>
            <label class="block text-sm font-medium text-dark mb-1" for="password">Password</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              autocomplete="current-password"
              placeholder="••••••••"
              class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
              [class.border-red]="isInvalid('password')"
            />
            @if (isInvalid('password')) {
              <p class="mt-1 text-xs text-red">Password is required.</p>
            }
          </div>

          <button
            type="submit"
            [disabled]="loading$ | async"
            class="w-full bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition disabled:opacity-60 disabled:cursor-not-allowed"
          >
            @if (loading$ | async) { Signing in… } @else { Sign In }
          </button>
        </form>

        <!-- Footer -->
        <p class="mt-4 text-center text-sm">
          <a routerLink="/auth/forgot-password" class="text-red hover:underline text-xs">Forgot password?</a>
        </p>
        <p class="mt-3 text-center text-sm text-muted">
          Don't have an account?
          <a routerLink="/auth/register" class="text-navy font-medium hover:underline">Register</a>
        </p>

        <!-- Social Login Divider -->
        <div class="mt-6 flex items-center gap-3">
          <div class="flex-1 border-t border-gray-200"></div>
          <span class="text-xs text-muted whitespace-nowrap">or continue with</span>
          <div class="flex-1 border-t border-gray-200"></div>
        </div>

        <!-- ENH-AUTH-001 — Facebook Login -->
        <button
          type="button"
          (click)="loginWithFacebook()"
          class="mt-4 w-full flex items-center justify-center gap-3 border border-gray-300 rounded-lg py-2.5 text-sm font-medium text-dark hover:bg-gray-50 transition"
        >
          <svg class="w-5 h-5" fill="#1877F2" viewBox="0 0 24 24" aria-hidden="true">
            <path
              d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094
                 10.125 24v-8.437H7.078v-3.49h3.047V9.41c0-3.025 1.792-4.697 4.533-4.697
                 1.312 0 2.686.235 2.686.235v2.97h-1.513c-1.491 0-1.956.93-1.956
                 1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z"
            />
          </svg>
          Continue with Facebook
        </button>

        <!-- ENH-AUTH-002 — Apple Sign-In (black button per Apple HIG) -->
        <button
          type="button"
          (click)="loginWithApple()"
          class="mt-3 w-full flex items-center justify-center gap-3 bg-[#1A1A1A] text-white rounded-lg py-2.5 text-sm font-medium hover:bg-black transition"
        >
          <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              d="M12.152 6.896c-.948 0-2.415-1.078-3.96-1.04-2.04.027-3.91 1.183-4.961
                 3.014-2.117 3.675-.54 9.103 1.519 12.09 1.013 1.453 2.208 3.09 3.792
                 3.039 1.52-.065 2.09-.987 3.935-.987 1.831 0 2.35.987 3.96.948
                 1.637-.026 2.676-1.48 3.676-2.948 1.156-1.688 1.636-3.325
                 1.662-3.415-.039-.013-3.182-1.221-3.22-4.857-.026-3.04 2.48-4.494
                 2.597-4.559-1.429-2.09-3.623-2.324-4.39-2.376-2-.156-3.675 1.09-4.61 1.09z
                 M15.53 3.83c.843-1.012 1.4-2.427 1.245-3.83-1.207.052-2.662.805-3.532
                 1.818-.78.896-1.454 2.338-1.273 3.714 1.338.104 2.715-.688 3.559-1.701"
            />
          </svg>
          Sign in with Apple
        </button>

        <!-- Admin hint -->
        <p class="mt-4 text-center text-xs text-muted">
          Admin: admin&#64;stylenest.com / Admin&#64;123
        </p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly store = inject(Store);
  private readonly fb    = inject(FormBuilder);

  readonly loading$ = this.store.select(selectAuthLoading);
  readonly error$   = this.store.select(selectAuthError);

  readonly form = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  isInvalid(field: 'email' | 'password'): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const { email, password } = this.form.getRawValue();
    this.store.dispatch(AuthActions.login({ email, password }));
  }

  /** ENH-AUTH-001 — triggers backend URL fetch then browser redirect to Facebook. */
  loginWithFacebook(): void {
    this.store.dispatch(AuthActions.facebookLogin());
  }

  /** ENH-AUTH-002 — triggers backend URL fetch then browser redirect to Apple. */
  loginWithApple(): void {
    this.store.dispatch(AuthActions.appleLogin());
  }
}
