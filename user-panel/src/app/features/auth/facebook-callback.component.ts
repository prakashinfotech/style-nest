import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError, selectMergeToken } from '../../store/auth/auth.selectors';

/**
 * ENH-AUTH-001 — Facebook OAuth 2.0 callback page.
 * Facebook redirects to /auth/facebook-callback?code=…&state=…
 * This component reads the code from the URL snapshot (no subscription needed),
 * dispatches facebookCallback, and:
 *   - NEW_ACCOUNT  → loginSuccess effect redirects home/admin
 *   - MERGE_REQUIRED → shows a password form to confirm account merge
 */
@Component({
  selector: 'app-facebook-callback',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, ReactiveFormsModule],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md bg-white rounded-2xl shadow-lg p-8">

        <!-- ── Error state ─────────────────────────────────────────────── -->
        @if (error$ | async; as error) {
          <div class="text-center">
            <div class="text-4xl mb-4">⚠️</div>
            <h2 class="text-lg font-semibold text-dark mb-2">Facebook Login Failed</h2>
            <p class="text-sm text-red mb-6">{{ error }}</p>
            <a
              href="/auth/login"
              class="inline-block bg-navy text-white text-sm font-medium px-6 py-2.5 rounded-lg hover:bg-blue transition"
            >
              Back to Login
            </a>
          </div>

        <!-- ── Merge required: password challenge ──────────────────────── -->
        } @else if (mergeToken$ | async; as mergeToken) {
          <div class="text-center mb-6">
            <div class="text-4xl mb-3">🔗</div>
            <h2 class="text-lg font-semibold text-dark">Link Your Account</h2>
            <p class="text-sm text-muted mt-1">
              An account already exists with this email. Enter your password to link your
              Facebook account.
            </p>
          </div>

          @if (error$ | async; as mergeError) {
            <div class="mb-4 px-4 py-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {{ mergeError }}
            </div>
          }

          <form [formGroup]="mergeForm" (ngSubmit)="submitMerge(mergeToken)" class="space-y-5" novalidate>
            <div>
              <label class="block text-sm font-medium text-dark mb-1" for="merge-password">
                Your Password
              </label>
              <input
                id="merge-password"
                type="password"
                formControlName="password"
                autocomplete="current-password"
                placeholder="••••••••"
                class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
                [class.border-red]="isMergePasswordInvalid()"
              />
              @if (isMergePasswordInvalid()) {
                <p class="mt-1 text-xs text-red">Password must be at least 8 characters.</p>
              }
            </div>

            <button
              type="submit"
              [disabled]="loading$ | async"
              class="w-full bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              @if (loading$ | async) { Linking account… } @else { Link &amp; Sign In }
            </button>
          </form>

        <!-- ── Loading / processing ────────────────────────────────────── -->
        } @else {
          <div class="text-center py-8">
            <div class="inline-flex items-center justify-center w-16 h-16 bg-navy/10 rounded-full mb-4">
              <!-- Facebook logo mark -->
              <svg class="w-8 h-8 text-navy" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094 10.125
                     24v-8.437H7.078v-3.49h3.047V9.41c0-3.025 1.792-4.697 4.533-4.697
                     1.312 0 2.686.235 2.686.235v2.97h-1.513c-1.491 0-1.956.93-1.956
                     1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z"
                />
              </svg>
            </div>
            <p class="text-dark font-medium">Signing you in with Facebook…</p>
            <p class="text-muted text-sm mt-1">Please wait</p>
            <!-- animated dots -->
            <div class="flex justify-center gap-1 mt-4">
              <span class="w-2 h-2 bg-navy rounded-full animate-bounce [animation-delay:-0.3s]"></span>
              <span class="w-2 h-2 bg-navy rounded-full animate-bounce [animation-delay:-0.15s]"></span>
              <span class="w-2 h-2 bg-navy rounded-full animate-bounce"></span>
            </div>
          </div>
        }

      </div>
    </div>
  `,
})
export class FacebookCallbackComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly fb    = inject(FormBuilder);

  readonly loading$    = this.store.select(selectAuthLoading);
  readonly error$      = this.store.select(selectAuthError);
  readonly mergeToken$ = this.store.select(selectMergeToken);

  readonly mergeForm = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    // Read URL params synchronously — no subscription required
    const code  = this.route.snapshot.queryParamMap.get('code');
    const error = this.route.snapshot.queryParamMap.get('error');

    if (error) {
      // Facebook denied access (user cancelled)
      this.store.dispatch(
        AuthActions.loginFailure({ error: 'Facebook login was cancelled or denied.' }),
      );
      return;
    }

    if (code) {
      this.store.dispatch(AuthActions.facebookCallback({ code }));
    } else {
      this.store.dispatch(
        AuthActions.loginFailure({ error: 'No authorization code received from Facebook.' }),
      );
    }
  }

  isMergePasswordInvalid(): boolean {
    const ctrl = this.mergeForm.get('password');
    return !!(ctrl?.invalid && ctrl.touched);
  }

  submitMerge(mergeToken: string): void {
    this.mergeForm.markAllAsTouched();
    if (this.mergeForm.invalid) return;
    const { password } = this.mergeForm.getRawValue();
    this.store.dispatch(AuthActions.facebookMergeConfirm({ mergeToken, password }));
  }
}
