import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError, selectMergeToken } from '../../store/auth/auth.selectors';

/**
 * ENH-AUTH-002 — Apple Sign-In callback page.
 * Apple uses response_mode=fragment, so the id_token arrives in the URL hash:
 *   /auth/apple-callback#id_token=…&state=…&token_type=Bearer
 *
 * ngOnInit reads window.location.hash synchronously (no subscription),
 * dispatches appleCallback, then either:
 *   NEW_ACCOUNT  → loginSuccess effect redirects home/admin
 *   MERGE_REQUIRED → shows password form to confirm account merge
 */
@Component({
  selector: 'app-apple-callback',
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
            <h2 class="text-lg font-semibold text-dark mb-2">Apple Sign-In Failed</h2>
            <p class="text-sm text-red mb-6">{{ error }}</p>
            <a
              href="/auth/login"
              class="inline-block bg-dark text-white text-sm font-medium px-6 py-2.5 rounded-lg hover:opacity-80 transition"
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
              Apple ID.
            </p>
          </div>

          @if (error$ | async; as mergeError) {
            <div class="mb-4 px-4 py-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {{ mergeError }}
            </div>
          }

          <form [formGroup]="mergeForm" (ngSubmit)="submitMerge(mergeToken)" class="space-y-5" novalidate>
            <div>
              <label class="block text-sm font-medium text-dark mb-1" for="apple-merge-password">
                Your Password
              </label>
              <input
                id="apple-merge-password"
                type="password"
                formControlName="password"
                autocomplete="current-password"
                placeholder="••••••••"
                class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-dark focus:ring-1 focus:ring-dark"
                [class.border-red]="isMergePasswordInvalid()"
              />
              @if (isMergePasswordInvalid()) {
                <p class="mt-1 text-xs text-red">Password must be at least 8 characters.</p>
              }
            </div>

            <button
              type="submit"
              [disabled]="loading$ | async"
              class="w-full bg-dark text-white font-semibold py-3 rounded-lg hover:opacity-80 transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              @if (loading$ | async) { Linking account… } @else { Link &amp; Sign In }
            </button>
          </form>

        <!-- ── Loading / processing ────────────────────────────────────── -->
        } @else {
          <div class="text-center py-8">
            <div class="inline-flex items-center justify-center w-16 h-16 bg-dark/10 rounded-full mb-4">
              <!-- Apple logo mark -->
              <svg class="w-8 h-8 text-dark" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
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
            </div>
            <p class="text-dark font-medium">Signing you in with Apple…</p>
            <p class="text-muted text-sm mt-1">Please wait</p>
            <div class="flex justify-center gap-1 mt-4">
              <span class="w-2 h-2 bg-dark rounded-full animate-bounce [animation-delay:-0.3s]"></span>
              <span class="w-2 h-2 bg-dark rounded-full animate-bounce [animation-delay:-0.15s]"></span>
              <span class="w-2 h-2 bg-dark rounded-full animate-bounce"></span>
            </div>
          </div>
        }

      </div>
    </div>
  `,
})
export class AppleCallbackComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb    = inject(FormBuilder);

  readonly loading$    = this.store.select(selectAuthLoading);
  readonly error$      = this.store.select(selectAuthError);
  readonly mergeToken$ = this.store.select(selectMergeToken);

  readonly mergeForm = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    // Apple delivers the id_token in the URL fragment — read synchronously, no subscription.
    // Fragment format: #id_token=…&state=…&token_type=Bearer
    const hash   = window.location.hash.slice(1); // strip leading '#'
    const params = new URLSearchParams(hash);

    const error   = params.get('error');
    const idToken = params.get('id_token');

    if (error) {
      this.store.dispatch(
        AuthActions.loginFailure({ error: 'Apple Sign-In was cancelled or denied.' }),
      );
      return;
    }

    if (idToken) {
      this.store.dispatch(AuthActions.appleCallback({ idToken }));
    } else {
      this.store.dispatch(
        AuthActions.loginFailure({ error: 'No identity token received from Apple.' }),
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
