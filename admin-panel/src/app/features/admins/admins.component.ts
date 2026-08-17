import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BehaviorSubject, Observable, catchError, of, switchMap } from 'rxjs';
import { AdminApiService, AdminStaff } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-admins',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, ReactiveFormsModule, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-dark">Admin Staff Management</h1>
        <button
          (click)="showForm.set(!showForm())"
          class="text-xs px-3 py-1.5 rounded-full bg-navy text-white border border-navy transition-colors hover:bg-navy/80">
          {{ showForm() ? 'Cancel' : '+ Add Admin' }}
        </button>
      </div>

      @if (showForm()) {
        <div class="bg-white rounded-xl shadow-sm border border-border p-5">
          <h2 class="font-semibold text-dark text-sm mb-4">Create Admin Account</h2>
          <form [formGroup]="form" (ngSubmit)="createAdmin()" class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs text-muted mb-1">First Name</label>
              <input formControlName="firstName" type="text" placeholder="First name"
                class="w-full border border-border rounded px-3 py-2 text-sm focus:outline-none focus:border-navy" />
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Last Name</label>
              <input formControlName="lastName" type="text" placeholder="Last name"
                class="w-full border border-border rounded px-3 py-2 text-sm focus:outline-none focus:border-navy" />
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Email</label>
              <input formControlName="email" type="email" placeholder="admin@example.com"
                class="w-full border border-border rounded px-3 py-2 text-sm focus:outline-none focus:border-navy" />
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Password</label>
              <input formControlName="password" type="password" placeholder="Min 8 characters"
                class="w-full border border-border rounded px-3 py-2 text-sm focus:outline-none focus:border-navy" />
            </div>
            <div class="md:col-span-2 flex gap-2 justify-end">
              <button type="submit" [disabled]="form.invalid"
                class="text-xs px-4 py-2 bg-navy text-white rounded hover:bg-navy/80 disabled:opacity-50 transition-colors">
                Create Admin
              </button>
            </div>
          </form>
        </div>
      }

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        @if (admins$ | async; as admins) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Name</th>
                  <th class="px-5 py-3 text-left">Email</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-left">Joined</th>
                  <th class="px-5 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (a of admins; track a.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ a.firstName }} {{ a.lastName }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ a.email }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="a.isActive ? 'active' : 'suspended'" /></td>
                    <td class="px-5 py-3 text-muted text-xs">{{ a.createdAt | date:'mediumDate' }}</td>
                    <td class="px-5 py-3 text-center">
                      @if (a.isActive) {
                        <button (click)="suspend(a.id)"
                          class="text-xs px-2 py-1 bg-error text-white rounded hover:bg-error/80 transition-colors">Suspend</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (!admins.length) {
            <div class="p-8 text-center text-muted text-sm">No admin staff found.</div>
          }
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading admin staff…</div>
        }
      </div>
    </div>
  `,
})
export class AdminsComponent implements OnInit {
  showForm = signal(false);
  refresh$ = new BehaviorSubject<void>(undefined);
  admins$: Observable<AdminStaff[]> | null = null;
  form: FormGroup;

  constructor(private api: AdminApiService, private fb: FormBuilder) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName:  ['', Validators.required],
      email:     ['', [Validators.required, Validators.email]],
      password:  ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  ngOnInit(): void {
    this.admins$ = this.refresh$.pipe(
      switchMap(() => this.api.getAdminStaff().pipe(catchError(() => of([]))))
    );
  }

  createAdmin(): void {
    if (this.form.invalid) return;
    const { email, password, firstName, lastName } = this.form.getRawValue() as {
      email: string; password: string; firstName: string; lastName: string;
    };
    this.api.createAdmin({ email, password, firstName, lastName }).subscribe(() => {
      this.showForm.set(false);
      this.form.reset();
      this.refresh$.next();
    });
  }

  suspend(id: string): void {
    this.api.suspendUser(id).subscribe(() => this.refresh$.next());
  }
}
