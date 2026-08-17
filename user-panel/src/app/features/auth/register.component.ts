import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../store/auth/auth.actions';
import { selectAuthLoading, selectAuthError } from '../../store/auth/auth.selectors';

function passwordsMatch(ctrl: AbstractControl): ValidationErrors | null {
  const pw  = ctrl.get('password')?.value;
  const cpw = ctrl.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { mismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4 py-8">
      <div class="w-full max-w-md bg-white rounded-2xl shadow-lg p-8">

        <!-- Header -->
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold text-navy">Create Account</h1>
          <p class="text-muted text-sm mt-1">Join StyleNest — shop the best brands</p>
        </div>

        <!-- Error banner -->
        @if (error$ | async; as error) {
          <div class="mb-4 px-4 py-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
            {{ error }}
          </div>
        }

        <!-- Form -->
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4" novalidate>

          <div class="flex gap-3">
            <div class="flex-1">
              <label class="block text-sm font-medium text-dark mb-1">First Name</label>
              <input
                type="text"
                formControlName="firstName"
                placeholder="First"
                class="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
                [class.border-red]="isInvalid('firstName')"
              />
              @if (isInvalid('firstName')) {
                <p class="mt-1 text-xs text-red">Required.</p>
              }
            </div>
            <div class="flex-1">
              <label class="block text-sm font-medium text-dark mb-1">Last Name</label>
              <input
                type="text"
                formControlName="lastName"
                placeholder="Last"
                class="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
                [class.border-red]="isInvalid('lastName')"
              />
              @if (isInvalid('lastName')) {
                <p class="mt-1 text-xs text-red">Required.</p>
              }
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-dark mb-1">Email</label>
            <input
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
            <label class="block text-sm font-medium text-dark mb-1">Password</label>
            <input
              type="password"
              formControlName="password"
              autocomplete="new-password"
              placeholder="Min 8 chars, 1 uppercase, 1 digit"
              class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
              [class.border-red]="isInvalid('password')"
            />
            @if (isInvalid('password')) {
              <p class="mt-1 text-xs text-red">Min 8 characters, 1 uppercase letter, 1 digit.</p>
            }
          </div>

          <div>
            <label class="block text-sm font-medium text-dark mb-1">Confirm Password</label>
            <input
              type="password"
              formControlName="confirmPassword"
              autocomplete="new-password"
              placeholder="Re-enter password"
              class="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:border-navy focus:ring-1 focus:ring-navy"
              [class.border-red]="isInvalid('confirmPassword') || (form.errors?.['mismatch'] && form.get('confirmPassword')?.touched)"
            />
            @if (form.errors?.['mismatch'] && form.get('confirmPassword')?.touched) {
              <p class="mt-1 text-xs text-red">Passwords do not match.</p>
            }
          </div>

          <button
            type="submit"
            [disabled]="loading$ | async"
            class="w-full bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition disabled:opacity-60 disabled:cursor-not-allowed mt-2"
          >
            @if (loading$ | async) { Creating account… } @else { Create Account }
          </button>
        </form>

        <p class="mt-6 text-center text-sm text-muted">
          Already have an account?
          <a routerLink="/auth/login" class="text-navy font-medium hover:underline">Sign In</a>
        </p>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private readonly store = inject(Store);
  private readonly fb    = inject(FormBuilder);

  readonly loading$ = this.store.select(selectAuthLoading);
  readonly error$   = this.store.select(selectAuthError);

  readonly form = this.fb.nonNullable.group(
    {
      firstName:       ['', Validators.required],
      lastName:        ['', Validators.required],
      email:           ['', [Validators.required, Validators.email]],
      password:        ['', [Validators.required, Validators.minLength(8), Validators.pattern(/(?=.*[A-Z])(?=.*\d)/)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch }
  );

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const { firstName, lastName, email, password } = this.form.getRawValue();
    this.store.dispatch(AuthActions.register({ firstName, lastName, email, password }));
  }
}
