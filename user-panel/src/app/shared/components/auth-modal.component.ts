import {
  ChangeDetectionStrategy, Component, HostListener,
  effect, inject, signal, computed,
} from '@angular/core';
import {
  ReactiveFormsModule, FormBuilder, Validators,
  AbstractControl, ValidationErrors,
} from '@angular/forms';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { selectOpenModalId, selectAuthModalMode } from '../../store/ui/ui.selectors';
import { selectAuthLoading, selectAuthError } from '../../store/auth/auth.selectors';
import { UiActions } from '../../store/ui/ui.actions';
import { AuthActions } from '../../store/auth/auth.actions';

function passwordsMatch(ctrl: AbstractControl): ValidationErrors | null {
  const pw  = ctrl.get('password')?.value as string;
  const cpw = ctrl.get('confirmPassword')?.value as string;
  return pw && cpw && pw !== cpw ? { mismatch: true } : null;
}

@Component({
  selector: 'app-auth-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  styles: [`
    .modal-overlay { animation: overlayIn 0.22s ease-out both; }
    .modal-card    { animation: cardIn 0.28s cubic-bezier(0.34, 1.4, 0.64, 1) both; }
    .modal-overlay.is-closing { animation: overlayOut 0.18s ease-in both; }
    .modal-card.is-closing    { animation: cardOut   0.18s ease-in both; }

    @keyframes overlayIn  { from { opacity: 0; }                                         to { opacity: 1; } }
    @keyframes overlayOut { from { opacity: 1; }                                         to { opacity: 0; } }
    @keyframes cardIn     { from { opacity: 0; transform: scale(0.88) translateY(24px); } to { opacity: 1; transform: scale(1) translateY(0); } }
    @keyframes cardOut    { from { opacity: 1; transform: scale(1)    translateY(0);    } to { opacity: 0; transform: scale(0.94) translateY(12px); } }

    .tab-slide-enter { animation: tabSlide 0.18s ease-out both; }
    @keyframes tabSlide { from { opacity: 0; transform: translateX(12px); } to { opacity: 1; transform: translateX(0); } }
  `],
  template: `
    @if (isOpen() || isClosing()) {
      <!-- Full-screen overlay -->
      <div
        class="fixed inset-0 z-[9999] flex items-end sm:items-center justify-center sm:p-4"
        role="presentation"
        [class.modal-overlay]="true"
        [class.is-closing]="isClosing()"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-black/60 backdrop-blur-[3px]"
          (click)="close()"
          aria-hidden="true"
        ></div>

        <!-- Modal card -->
        <div
          class="modal-card relative w-full sm:max-w-md bg-white sm:rounded-2xl rounded-t-2xl shadow-2xl overflow-hidden max-h-[95dvh] overflow-y-auto"
          [class.is-closing]="isClosing()"
          role="dialog"
          aria-modal="true"
          [attr.aria-labelledby]="activeTab() === 'login' ? 'auth-modal-title-login' : 'auth-modal-title-register'"
        >
          <!-- Mobile drag handle -->
          <div class="sm:hidden flex justify-center pt-2 pb-0 flex-shrink-0">
            <div class="w-10 h-1 rounded-full bg-border"></div>
          </div>

          <!-- Close button -->
          <button
            type="button"
            class="absolute top-5 right-4 z-10 w-8 h-8 flex items-center justify-center
                   rounded-full bg-bg hover:bg-border text-muted hover:text-dark
                   transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-navy/30"
            aria-label="Close dialog"
            (click)="close()"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>

          <!-- Brand header -->
          <div class="px-6 sm:px-8 pt-5 sm:pt-7 pb-1 text-center">
            <div class="inline-flex items-baseline gap-1.5">
              <span class="text-xl font-bold tracking-tight font-display">
                <span class="text-navy">TATA</span>&nbsp;<span class="text-red">StyleNest</span>
              </span>
              <span class="text-[11px] text-muted font-medium tracking-widest uppercase">Fashion</span>
            </div>
            <h2
              [id]="activeTab() === 'login' ? 'auth-modal-title-login' : 'auth-modal-title-register'"
              class="mt-1 text-base font-semibold text-dark"
            >
              {{ activeTab() === 'login' ? 'Welcome back' : 'Join the community' }}
            </h2>
          </div>

          <!-- Tab switcher -->
          <div class="px-6 sm:px-8 mt-4">
            <div class="flex bg-bg rounded-xl p-1 gap-1">
              <button
                type="button"
                class="flex-1 py-2.5 rounded-lg text-sm font-semibold tracking-wide transition-all duration-200
                       focus:outline-none focus:ring-2 focus:ring-navy/30"
                [class.bg-white]="activeTab() === 'login'"
                [class.text-navy]="activeTab() === 'login'"
                [class.shadow-sm]="activeTab() === 'login'"
                [class.text-muted]="activeTab() !== 'login'"
                (click)="switchTab('login')"
                [attr.aria-selected]="activeTab() === 'login'"
              >Sign In</button>
              <button
                type="button"
                class="flex-1 py-2.5 rounded-lg text-sm font-semibold tracking-wide transition-all duration-200
                       focus:outline-none focus:ring-2 focus:ring-navy/30"
                [class.bg-white]="activeTab() === 'register'"
                [class.text-navy]="activeTab() === 'register'"
                [class.shadow-sm]="activeTab() === 'register'"
                [class.text-muted]="activeTab() !== 'register'"
                (click)="switchTab('register')"
                [attr.aria-selected]="activeTab() === 'register'"
              >Register</button>
            </div>
          </div>

          <!-- Form area -->
          <div class="px-6 sm:px-8 py-5 sm:py-6 tab-slide-enter" [attr.key]="activeTab()">

            <!-- Server error banner -->
            @if (error()) {
              <div class="mb-4 px-4 py-3 bg-red-50 border border-red-200 rounded-xl flex items-start gap-2.5" role="alert">
                <svg class="w-4 h-4 text-red flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
                <p class="text-red-700 text-sm">{{ error() }}</p>
              </div>
            }

            <!-- ── LOGIN FORM ── -->
            @if (activeTab() === 'login') {
              <form [formGroup]="loginForm" (ngSubmit)="submitLogin()" class="space-y-4" novalidate>

                <!-- Email -->
                <div>
                  <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide" for="lf-email">
                    Email Address
                  </label>
                  <input
                    id="lf-email"
                    type="email"
                    formControlName="email"
                    autocomplete="email"
                    placeholder="you@example.com"
                    class="w-full border rounded-xl px-4 py-3 text-sm text-dark placeholder-muted
                           focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                           transition-all duration-150"
                    [class.border-border]="!loginFieldInvalid('email')"
                    [class.border-red]="loginFieldInvalid('email')"
                    [class.bg-red-50]="loginFieldInvalid('email')"
                  />
                  @if (loginFieldInvalid('email')) {
                    <p class="mt-1.5 text-xs text-red flex items-center gap-1" role="alert">
                      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                      </svg>
                      Enter a valid email address
                    </p>
                  }
                </div>

                <!-- Password -->
                <div>
                  <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide" for="lf-password">
                    Password
                  </label>
                  <div class="relative">
                    <input
                      id="lf-password"
                      [type]="showLoginPassword() ? 'text' : 'password'"
                      formControlName="password"
                      autocomplete="current-password"
                      placeholder="Your password"
                      class="w-full border rounded-xl px-4 py-3 pr-11 text-sm text-dark placeholder-muted
                             focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                             transition-all duration-150"
                      [class.border-border]="!loginFieldInvalid('password')"
                      [class.border-red]="loginFieldInvalid('password')"
                      [class.bg-red-50]="loginFieldInvalid('password')"
                    />
                    <button
                      type="button"
                      class="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-dark
                             transition-colors p-1 rounded focus:outline-none focus:ring-1 focus:ring-navy/30"
                      [attr.aria-label]="showLoginPassword() ? 'Hide password' : 'Show password'"
                      (click)="showLoginPassword.set(!showLoginPassword())"
                    >
                      @if (showLoginPassword()) {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                        </svg>
                      } @else {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                        </svg>
                      }
                    </button>
                  </div>
                  @if (loginFieldInvalid('password')) {
                    <p class="mt-1.5 text-xs text-red flex items-center gap-1" role="alert">
                      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                      </svg>
                      Password is required
                    </p>
                  }
                </div>

                <!-- Submit -->
                <button
                  type="submit"
                  class="w-full bg-navy text-white font-semibold py-3.5 rounded-xl
                         hover:bg-opacity-90 active:scale-[0.98]
                         transition-all duration-150 mt-2
                         disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100
                         flex items-center justify-center gap-2.5
                         focus:outline-none focus:ring-2 focus:ring-navy/40"
                  [disabled]="isLoading()"
                >
                  @if (isLoading()) {
                    <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                    </svg>
                    Signing in…
                  } @else {
                    Sign In
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3"/>
                    </svg>
                  }
                </button>
              </form>

              <!-- Admin hint -->
              <div class="mt-4 p-3 bg-bg rounded-xl border border-border">
                <p class="text-xs text-muted text-center leading-relaxed">
                  <span class="font-semibold text-navy">Admin test account:</span><br/>
                  admin&#64;stylenest.com &nbsp;/&nbsp; Admin&#64;123
                </p>
              </div>
            }

            <!-- ── REGISTER FORM ── -->
            @if (activeTab() === 'register') {
              <form [formGroup]="registerForm" (ngSubmit)="submitRegister()" class="space-y-4" novalidate>

                <!-- Name row -->
                <div class="flex gap-3">
                  <div class="flex-1">
                    <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide">
                      First Name
                    </label>
                    <input
                      type="text"
                      formControlName="firstName"
                      placeholder="First"
                      autocomplete="given-name"
                      class="w-full border rounded-xl px-4 py-3 text-sm text-dark placeholder-muted
                             focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                             transition-all duration-150"
                      [class.border-border]="!registerFieldInvalid('firstName')"
                      [class.border-red]="registerFieldInvalid('firstName')"
                      [class.bg-red-50]="registerFieldInvalid('firstName')"
                    />
                    @if (registerFieldInvalid('firstName')) {
                      <p class="mt-1 text-xs text-red" role="alert">Required</p>
                    }
                  </div>
                  <div class="flex-1">
                    <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide">
                      Last Name
                    </label>
                    <input
                      type="text"
                      formControlName="lastName"
                      placeholder="Last"
                      autocomplete="family-name"
                      class="w-full border rounded-xl px-4 py-3 text-sm text-dark placeholder-muted
                             focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                             transition-all duration-150"
                      [class.border-border]="!registerFieldInvalid('lastName')"
                      [class.border-red]="registerFieldInvalid('lastName')"
                      [class.bg-red-50]="registerFieldInvalid('lastName')"
                    />
                    @if (registerFieldInvalid('lastName')) {
                      <p class="mt-1 text-xs text-red" role="alert">Required</p>
                    }
                  </div>
                </div>

                <!-- Email -->
                <div>
                  <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide" for="rf-email">
                    Email Address
                  </label>
                  <input
                    id="rf-email"
                    type="email"
                    formControlName="email"
                    autocomplete="email"
                    placeholder="you@example.com"
                    class="w-full border rounded-xl px-4 py-3 text-sm text-dark placeholder-muted
                           focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                           transition-all duration-150"
                    [class.border-border]="!registerFieldInvalid('email')"
                    [class.border-red]="registerFieldInvalid('email')"
                    [class.bg-red-50]="registerFieldInvalid('email')"
                  />
                  @if (registerFieldInvalid('email')) {
                    <p class="mt-1.5 text-xs text-red flex items-center gap-1" role="alert">
                      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                      </svg>
                      Enter a valid email address
                    </p>
                  }
                </div>

                <!-- Password -->
                <div>
                  <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide" for="rf-password">
                    Password
                  </label>
                  <div class="relative">
                    <input
                      id="rf-password"
                      [type]="showRegisterPassword() ? 'text' : 'password'"
                      formControlName="password"
                      autocomplete="new-password"
                      placeholder="Min 8 chars, 1 uppercase, 1 digit"
                      class="w-full border rounded-xl px-4 py-3 pr-11 text-sm text-dark placeholder-muted
                             focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                             transition-all duration-150"
                      [class.border-border]="!registerFieldInvalid('password')"
                      [class.border-red]="registerFieldInvalid('password')"
                      [class.bg-red-50]="registerFieldInvalid('password')"
                    />
                    <button
                      type="button"
                      class="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-dark
                             transition-colors p-1 rounded focus:outline-none focus:ring-1 focus:ring-navy/30"
                      [attr.aria-label]="showRegisterPassword() ? 'Hide password' : 'Show password'"
                      (click)="showRegisterPassword.set(!showRegisterPassword())"
                    >
                      @if (showRegisterPassword()) {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                        </svg>
                      } @else {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                        </svg>
                      }
                    </button>
                  </div>
                  @if (registerFieldInvalid('password')) {
                    <p class="mt-1.5 text-xs text-red flex items-center gap-1" role="alert">
                      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                      </svg>
                      Min 8 characters, 1 uppercase letter, 1 digit
                    </p>
                  }
                </div>

                <!-- Confirm Password -->
                <div>
                  <label class="block text-xs font-semibold text-dark mb-1.5 uppercase tracking-wide" for="rf-confirm">
                    Confirm Password
                  </label>
                  <div class="relative">
                    <input
                      id="rf-confirm"
                      [type]="showConfirmPassword() ? 'text' : 'password'"
                      formControlName="confirmPassword"
                      autocomplete="new-password"
                      placeholder="Re-enter your password"
                      class="w-full border rounded-xl px-4 py-3 pr-11 text-sm text-dark placeholder-muted
                             focus:outline-none focus:ring-2 focus:ring-navy/20 focus:border-navy
                             transition-all duration-150"
                      [class.border-border]="!(registerForm.errors?.['mismatch'] && registerForm.get('confirmPassword')?.touched)"
                      [class.border-red]="registerForm.errors?.['mismatch'] && registerForm.get('confirmPassword')?.touched"
                      [class.bg-red-50]="registerForm.errors?.['mismatch'] && registerForm.get('confirmPassword')?.touched"
                    />
                    <button
                      type="button"
                      class="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-dark
                             transition-colors p-1 rounded focus:outline-none focus:ring-1 focus:ring-navy/30"
                      [attr.aria-label]="showConfirmPassword() ? 'Hide password' : 'Show password'"
                      (click)="showConfirmPassword.set(!showConfirmPassword())"
                    >
                      @if (showConfirmPassword()) {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                        </svg>
                      } @else {
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                        </svg>
                      }
                    </button>
                  </div>
                  @if (registerForm.errors?.['mismatch'] && registerForm.get('confirmPassword')?.touched) {
                    <p class="mt-1.5 text-xs text-red flex items-center gap-1" role="alert">
                      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                      </svg>
                      Passwords do not match
                    </p>
                  }
                </div>

                <!-- Submit -->
                <button
                  type="submit"
                  class="w-full bg-navy text-white font-semibold py-3.5 rounded-xl
                         hover:bg-opacity-90 active:scale-[0.98]
                         transition-all duration-150 mt-2
                         disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100
                         flex items-center justify-center gap-2.5
                         focus:outline-none focus:ring-2 focus:ring-navy/40"
                  [disabled]="isLoading()"
                >
                  @if (isLoading()) {
                    <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                    </svg>
                    Creating account…
                  } @else {
                    Create Account
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3"/>
                    </svg>
                  }
                </button>
              </form>
            }

            <!-- Tab switch link -->
            <p class="mt-5 text-center text-sm text-muted">
              @if (activeTab() === 'login') {
                New to TATA StyleNest?
                <button
                  type="button"
                  class="text-navy font-semibold hover:underline ml-1 focus:outline-none focus:underline"
                  (click)="switchTab('register')"
                >Create an account</button>
              } @else {
                Already have an account?
                <button
                  type="button"
                  class="text-navy font-semibold hover:underline ml-1 focus:outline-none focus:underline"
                  (click)="switchTab('login')"
                >Sign in</button>
              }
            </p>

          </div>
        </div>
      </div>
    }
  `,
})
export class AuthModalComponent {
  private readonly store = inject(Store);
  private readonly fb    = inject(FormBuilder);

  private readonly openModalId  = toSignal(this.store.select(selectOpenModalId),   { initialValue: null as string | null });
  private readonly authModalMode = toSignal(this.store.select(selectAuthModalMode), { initialValue: 'login' as 'login' | 'register' });

  readonly isLoading = toSignal(this.store.select(selectAuthLoading), { initialValue: false });
  readonly error     = toSignal(this.store.select(selectAuthError),   { initialValue: null as string | null });

  readonly isOpen    = computed(() => this.openModalId() === 'auth');
  readonly isClosing = signal(false);

  readonly activeTab           = signal<'login' | 'register'>('login');
  readonly showLoginPassword   = signal(false);
  readonly showRegisterPassword = signal(false);
  readonly showConfirmPassword  = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  readonly registerForm = this.fb.nonNullable.group(
    {
      firstName:       ['', Validators.required],
      lastName:        ['', Validators.required],
      email:           ['', [Validators.required, Validators.email]],
      password:        ['', [Validators.required, Validators.minLength(8), Validators.pattern(/(?=.*[A-Z])(?=.*\d)/)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.activeTab.set(this.authModalMode() ?? 'login');
        this.loginForm.reset();
        this.registerForm.reset();
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen() && !this.isClosing()) this.close();
  }

  close(): void {
    if (this.isClosing()) return;
    this.isClosing.set(true);
    this.store.dispatch(AuthActions.clearError());
    setTimeout(() => {
      this.isClosing.set(false);
      this.store.dispatch(UiActions.closeModal());
    }, 180);
  }

  switchTab(tab: 'login' | 'register'): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    this.store.dispatch(AuthActions.clearError());
    this.loginForm.markAsUntouched();
    this.registerForm.markAsUntouched();
  }

  loginFieldInvalid(field: 'email' | 'password'): boolean {
    const ctrl = this.loginForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  registerFieldInvalid(field: string): boolean {
    const ctrl = this.registerForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  submitLogin(): void {
    this.loginForm.markAllAsTouched();
    if (this.loginForm.invalid) return;
    const { email, password } = this.loginForm.getRawValue();
    this.store.dispatch(AuthActions.login({ email, password }));
  }

  submitRegister(): void {
    this.registerForm.markAllAsTouched();
    if (this.registerForm.invalid) return;
    const { firstName, lastName, email, password } = this.registerForm.getRawValue();
    this.store.dispatch(AuthActions.register({ firstName, lastName, email, password }));
  }
}
