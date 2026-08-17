/**
 * ENH-ADMIN-002 — Hangfire Dashboard admin-only route + Job Management UI.
 *
 * Displays all registered scheduled jobs and allows admins to trigger them
 * on demand via POST /api/v1/admin/jobs/{slug}/run.  A link to the Hangfire
 * dashboard is provided for Smtp-mode deployments where the Auth.API exposes it.
 */
import {
  ChangeDetectionStrategy, Component, inject, signal,
} from '@angular/core';
import { AsyncPipe, NgClass } from '@angular/common';
import { catchError, finalize, EMPTY } from 'rxjs';
import { AdminService, AdminJob, JobRunResult } from '../../core/services/admin.service';

interface JobState {
  running: boolean;
  result:  JobRunResult | null;
  error:   string | null;
}

@Component({
  selector: 'app-admin-job-management',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, NgClass],
  template: `
    <div class="p-4 md:p-8">

      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-6">
        <div>
          <h1 class="text-xl md:text-2xl font-display font-bold text-dark">Job Management</h1>
          <p class="text-sm text-muted mt-1">Trigger scheduled background jobs on demand.</p>
        </div>

        <!-- Hangfire Dashboard deep link -->
        <a
          [href]="hangfireUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="inline-flex items-center gap-2 text-sm font-medium text-blue hover:underline"
          title="Open Hangfire Dashboard (admin JWT required)"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4
                 M14 4h6m0 0v6m0-6L10 14"/>
          </svg>
          Hangfire Dashboard
        </a>
      </div>

      <!-- Jobs table -->
      @if (jobs$ | async; as jobs) {
        @if (jobs.length === 0) {
          <div class="bg-card border border-border rounded-xl p-10 text-center text-muted text-sm">
            No scheduled jobs registered.
          </div>
        } @else {
          <div class="bg-card border border-border rounded-xl overflow-hidden">

            <!-- Header row -->
            <div class="grid grid-cols-[1fr_auto] md:grid-cols-[1fr_180px_auto] gap-4
                        px-5 py-3 bg-bg border-b border-border text-xs font-semibold
                        text-muted uppercase tracking-widest">
              <span>Job</span>
              <span class="hidden md:block">Last Run Result</span>
              <span>Actions</span>
            </div>

            @for (job of jobs; track job.slug) {
              <div class="grid grid-cols-[1fr_auto] md:grid-cols-[1fr_180px_auto] gap-4
                          px-5 py-4 items-center border-b border-border last:border-0
                          hover:bg-bg/60 transition-colors">

                <!-- Job identity -->
                <div>
                  <p class="font-medium text-dark text-sm">{{ job.name }}</p>
                  <code class="text-xs text-muted">{{ job.slug }}</code>
                </div>

                <!-- Last run result (desktop only) -->
                <div class="hidden md:block text-xs">
                  @if (jobState(job.slug); as s) {
                    @if (s.result) {
                      <span
                        [ngClass]="s.result.success
                          ? 'bg-green-100 text-success'
                          : 'bg-red-100 text-red'"
                        class="px-2 py-0.5 rounded-full font-medium"
                      >
                        {{ s.result.success ? '✓ Succeeded' : '✗ Failed' }}
                      </span>
                      <p class="text-muted mt-1 truncate max-w-[160px]" [title]="s.result.message">
                        {{ s.result.message }}
                      </p>
                    } @else if (s.error) {
                      <span class="text-red">{{ s.error }}</span>
                    } @else {
                      <span class="text-muted">—</span>
                    }
                  } @else {
                    <span class="text-muted">—</span>
                  }
                </div>

                <!-- Run button -->
                <button
                  type="button"
                  (click)="runJob(job)"
                  [disabled]="jobState(job.slug)?.running"
                  class="px-4 py-1.5 rounded-md text-sm font-medium transition-colors
                         bg-navy text-white hover:bg-navy/90 active:scale-[0.97]
                         disabled:opacity-50 disabled:cursor-not-allowed
                         min-w-[88px] flex items-center justify-center gap-1.5"
                >
                  @if (jobState(job.slug)?.running) {
                    <!-- Spinner -->
                    <svg class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                      <circle class="opacity-25" cx="12" cy="12" r="10"
                              stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor"
                            d="M4 12a8 8 0 018-8v8H4z"></path>
                    </svg>
                    Running…
                  } @else {
                    Run Now
                  }
                </button>
              </div>
            }
          </div>
        }
      } @else {
        <!-- Loading skeleton -->
        <div class="space-y-2">
          @for (i of [0,1,2,3]; track i) {
            <div class="h-14 bg-border rounded-xl animate-pulse"></div>
          }
        </div>
      }

      <!-- Mobile result toast area -->
      @if (lastToast()) {
        <div
          class="fixed bottom-6 inset-x-4 sm:inset-x-auto sm:right-6 sm:left-auto sm:w-80
                 bg-dark text-white text-sm rounded-xl shadow-lg p-4 z-50
                 flex items-start gap-3"
          role="alert"
        >
          <span class="text-lg leading-none mt-px">
            {{ lastToast()!.success ? '✓' : '✗' }}
          </span>
          <div class="flex-1 min-w-0">
            <p class="font-medium">{{ lastToast()!.jobName }}</p>
            <p class="text-white/70 truncate">{{ lastToast()!.message }}</p>
          </div>
          <button
            type="button"
            (click)="dismissToast()"
            class="text-white/50 hover:text-white ml-2 shrink-0"
            aria-label="Dismiss"
          >✕</button>
        </div>
      }

    </div>
  `,
})
export class AdminJobManagementComponent {
  private readonly adminService = inject(AdminService);

  /** All registered scheduled jobs from the Admin.API. */
  readonly jobs$ = this.adminService.getAdminJobs();

  /** Per-job run state (keyed by slug). */
  private readonly _states = signal<Record<string, JobState>>({});

  /** Toast shown after each run (cleared on dismiss or next run). */
  readonly lastToast = signal<JobRunResult | null>(null);

  /**
   * Deep link to the Hangfire dashboard proxied via the YARP gateway at /hangfire.
   * Auth.API requires Admin/SuperAdmin role (JWT Bearer) or loopback access (dev).
   */
  readonly hangfireUrl = '/hangfire';

  jobState(slug: string): JobState | undefined {
    return this._states()[slug];
  }

  runJob(job: AdminJob): void {
    if (this._states()[job.slug]?.running) return;

    // Set running state
    this._states.update((s) => ({
      ...s,
      [job.slug]: { running: true, result: null, error: null },
    }));
    this.lastToast.set(null);

    this.adminService.runAdminJob(job.slug).pipe(
      catchError((err: unknown) => {
        const msg = (err as { error?: { message?: string }; message?: string })
          ?.error?.message ?? (err as { message?: string })?.message ?? 'Unknown error';
        this._states.update((s) => ({
          ...s,
          [job.slug]: { running: false, result: null, error: msg },
        }));
        return EMPTY;
      }),
      finalize(() => {
        this._states.update((s) => ({
          ...s,
          [job.slug]: { ...s[job.slug], running: false },
        }));
      }),
    ).subscribe((result) => {
      this._states.update((s) => ({
        ...s,
        [job.slug]: { running: false, result, error: null },
      }));
      this.lastToast.set(result);
      // Auto-dismiss toast after 5 s
      setTimeout(() => this.lastToast.set(null), 5000);
    });
  }

  dismissToast(): void {
    this.lastToast.set(null);
  }
}
