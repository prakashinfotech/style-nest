import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import {
  AdminService,
  AdminUser,
  CreateSellerRequest,
} from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, ReactiveFormsModule],
  template: `
    <div class="p-4 md:p-8">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl md:text-2xl font-display font-bold text-dark">Users</h1>
        <button (click)="toggleForm()"
                class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium
                       bg-navy text-white hover:bg-navy/90 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  [attr.d]="showForm() ? 'M6 18L18 6M6 6l12 12' : 'M12 4v16m8-8H4'"/>
          </svg>
          {{ showForm() ? 'Cancel' : 'Create Seller' }}
        </button>
      </div>

      <!-- Create Seller Form Panel -->
      @if (showForm()) {
        <div class="bg-card rounded-xl border border-border p-5 mb-6">
          <h2 class="text-sm font-semibold text-dark mb-4">New Seller Account</h2>
          <form [formGroup]="sellerForm" (ngSubmit)="submitSeller()"
                class="grid grid-cols-1 sm:grid-cols-2 gap-4">

            <div>
              <label class="block text-xs font-medium text-muted mb-1">First Name</label>
              <input formControlName="firstName"
                     placeholder="Jane"
                     class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2
                            focus:ring-navy/30 border-border bg-bg text-dark"
                     [class.border-red]="isInvalid('firstName')"/>
              @if (isInvalid('firstName')) {
                <p class="text-xs text-red mt-1">Required</p>
              }
            </div>

            <div>
              <label class="block text-xs font-medium text-muted mb-1">Last Name</label>
              <input formControlName="lastName"
                     placeholder="Doe"
                     class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2
                            focus:ring-navy/30 border-border bg-bg text-dark"
                     [class.border-red]="isInvalid('lastName')"/>
              @if (isInvalid('lastName')) {
                <p class="text-xs text-red mt-1">Required</p>
              }
            </div>

            <div>
              <label class="block text-xs font-medium text-muted mb-1">Email</label>
              <input formControlName="email"
                     type="email"
                     placeholder="seller@example.com"
                     class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2
                            focus:ring-navy/30 border-border bg-bg text-dark"
                     [class.border-red]="isInvalid('email')"/>
              @if (isInvalid('email')) {
                <p class="text-xs text-red mt-1">Valid email required</p>
              }
            </div>

            <div>
              <label class="block text-xs font-medium text-muted mb-1">Password</label>
              <input formControlName="password"
                     type="password"
                     placeholder="Min 8 chars, 1 uppercase, 1 digit"
                     class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2
                            focus:ring-navy/30 border-border bg-bg text-dark"
                     [class.border-red]="isInvalid('password')"/>
              @if (isInvalid('password')) {
                <p class="text-xs text-red mt-1">Min 8 characters required</p>
              }
            </div>

            <div class="sm:col-span-2 flex items-center gap-3">
              <button type="submit"
                      [disabled]="submitting()"
                      class="px-5 py-2 rounded-lg text-sm font-medium bg-navy text-white
                             hover:bg-navy/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
                {{ submitting() ? 'Creating…' : 'Create Seller Account' }}
              </button>

              @if (formError()) {
                <p class="text-sm text-red">{{ formError() }}</p>
              }
              @if (formSuccess()) {
                <p class="text-sm text-success">{{ formSuccess() }}</p>
              }
            </div>
          </form>
        </div>
      }

      @if (users$ | async; as users) {
        <!-- Summary row -->
        <div class="grid grid-cols-2 sm:grid-cols-3 gap-3 mb-6">
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-2xl font-bold text-navy">{{ users.length }}</p>
            <p class="text-xs text-muted mt-1">Total Users</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-2xl font-bold text-gold">{{ sellerCount(users) }}</p>
            <p class="text-xs text-muted mt-1">Sellers</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-4 text-center hidden sm:block">
            <p class="text-2xl font-bold text-red">{{ adminCount(users) }}</p>
            <p class="text-xs text-muted mt-1">Admins</p>
          </div>
        </div>

        @if (users.length === 0) {
          <div class="bg-card rounded-xl border border-border p-12 text-center text-muted">
            No users found.
          </div>
        } @else {
          <div class="bg-card rounded-xl border border-border overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-bg border-b border-border">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Name</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Email</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Roles</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden lg:table-cell">Verified</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden lg:table-cell">Joined</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border">
                  @for (user of users; track user.id) {
                    <tr class="hover:bg-bg/50 transition-colors">
                      <td class="px-4 py-3">
                        <p class="font-medium text-dark">{{ user.firstName }} {{ user.lastName }}</p>
                        <p class="text-xs text-muted md:hidden">{{ user.email }}</p>
                      </td>
                      <td class="px-4 py-3 text-muted hidden md:table-cell">{{ user.email }}</td>
                      <td class="px-4 py-3">
                        <div class="flex flex-wrap gap-1">
                          @for (role of user.roles; track role) {
                            <span [class]="roleClass(role)">{{ role }}</span>
                          }
                        </div>
                      </td>
                      <td class="px-4 py-3 hidden lg:table-cell">
                        <span [class]="user.emailConfirmed ? 'text-xs text-success' : 'text-xs text-muted'">
                          {{ user.emailConfirmed ? '✓ Verified' : '— Unverified' }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-muted text-xs hidden lg:table-cell">
                        {{ user.createdAt | date:'dd MMM yyyy' }}
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      } @else {
        <div class="space-y-2">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="h-12 bg-border/40 rounded-lg animate-pulse"></div>
          }
        </div>
      }
    </div>
  `,
})
export class AdminUsersComponent {
  private readonly adminService = inject(AdminService);
  private readonly fb           = inject(FormBuilder);
  private readonly cdr          = inject(ChangeDetectorRef);

  readonly users$: Observable<AdminUser[]> = this.adminService.getAdminUsers();

  readonly showForm  = signal(false);
  readonly submitting = signal(false);
  readonly formError  = signal('');
  readonly formSuccess = signal('');

  readonly sellerForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(8)]],
  });

  toggleForm(): void {
    this.showForm.update((v) => !v);
    this.formError.set('');
    this.formSuccess.set('');
    if (!this.showForm()) this.sellerForm.reset();
  }

  isInvalid(field: keyof typeof this.sellerForm.controls): boolean {
    const ctrl = this.sellerForm.controls[field];
    return ctrl.invalid && ctrl.touched;
  }

  submitSeller(): void {
    if (this.sellerForm.invalid) {
      this.sellerForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set('');
    this.formSuccess.set('');

    const req: CreateSellerRequest = this.sellerForm.getRawValue();

    this.adminService.createSellerAccount(req).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.formSuccess.set(`Seller account created for ${res.email}`);
        this.sellerForm.reset();
        this.cdr.markForCheck();
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        const msg =
          typeof err === 'object' && err !== null && 'error' in err
            ? ((err as { error?: { message?: string } }).error?.message ?? 'Failed to create seller account.')
            : 'Failed to create seller account.';
        this.formError.set(msg);
        this.cdr.markForCheck();
      },
    });
  }

  sellerCount(users: AdminUser[]): number {
    return users.filter((u) => u.roles.includes('Seller')).length;
  }

  adminCount(users: AdminUser[]): number {
    return users.filter((u) => u.roles.includes('Admin')).length;
  }

  roleClass(role: string): string {
    const base = 'text-xs font-medium px-2 py-0.5 rounded-full';
    switch (role) {
      case 'Admin':  return `${base} bg-navy/10 text-navy`;
      case 'Seller': return `${base} bg-gold/10 text-gold`;
      default:       return `${base} bg-muted/10 text-muted`;
    }
  }
}
