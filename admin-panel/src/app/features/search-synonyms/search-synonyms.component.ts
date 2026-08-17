/**
 * ENH-ADMIN-006 — Search Synonym Management.
 *
 * Admin CMS page for managing the search synonym dictionary used by
 * the Catalog search-suggest endpoint (ENH-SRCH-003).
 *
 * Features:
 *  - Table listing all synonym entries (term → synonyms)
 *  - Add new entry: term + comma-separated synonyms → PUT
 *  - Edit existing: inline edit panel → PUT (upsert)
 *  - Delete: with confirmation notice → DELETE
 */

import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, of } from 'rxjs';
import {
  AdminApiService,
  SearchSynonymDto,
} from '../../core/services/admin-api.service';

@Component({
  selector: 'app-search-synonyms',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DatePipe, ReactiveFormsModule],
  template: `
    <div class="space-y-6">

      <!-- Page header -->
      <div class="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 class="text-xl font-bold text-dark">Search Synonyms</h1>
          <p class="text-sm text-muted mt-0.5">
            Map search terms to alternate synonyms so shoppers find results regardless of phrasing.
          </p>
        </div>
        <button
          type="button"
          class="px-4 py-2 bg-navy text-white text-sm font-medium rounded hover:bg-navy/90 transition
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
          (click)="openAdd()"
        >
          + Add Synonym
        </button>
      </div>

      <!-- Add / Edit form panel -->
      @if (showForm()) {
        <div class="bg-white rounded-xl border border-border shadow-sm p-5">
          <h2 class="text-sm font-semibold text-dark mb-4">
            {{ editingTerm() ? 'Edit Synonym — ' + editingTerm() : 'New Synonym Entry' }}
          </h2>

          <form [formGroup]="form" (ngSubmit)="onSave()" novalidate class="space-y-4">

            <!-- Term -->
            <div>
              <label for="syn-term" class="block text-xs font-medium text-dark mb-1">
                Term <span class="text-red-600" aria-hidden="true">*</span>
              </label>
              <input
                id="syn-term"
                type="text"
                formControlName="term"
                placeholder="e.g. tshirt"
                class="w-full max-w-xs border border-border rounded px-3 py-2 text-sm text-dark
                       placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-navy bg-white"
                [attr.aria-invalid]="form.controls.term.invalid && form.controls.term.touched"
                [readonly]="!!editingTerm()"
                [class.bg-bg]="!!editingTerm()"
              />
              @if (form.controls.term.invalid && form.controls.term.touched) {
                <p class="text-xs text-red-600 mt-1" role="alert">Term is required (max 200 chars).</p>
              }
              @if (editingTerm()) {
                <p class="text-xs text-muted mt-1">Term cannot be changed once created.</p>
              }
            </div>

            <!-- Synonyms (comma-separated) -->
            <div>
              <label for="syn-synonyms" class="block text-xs font-medium text-dark mb-1">
                Synonyms <span class="text-muted font-normal">(comma-separated)</span>
                <span class="text-red-600" aria-hidden="true">*</span>
              </label>
              <input
                id="syn-synonyms"
                type="text"
                formControlName="synonyms"
                placeholder="e.g. t-shirt, tee, top"
                class="w-full max-w-sm border border-border rounded px-3 py-2 text-sm text-dark
                       placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-navy bg-white"
                [attr.aria-invalid]="form.controls.synonyms.invalid && form.controls.synonyms.touched"
              />
              @if (form.controls.synonyms.invalid && form.controls.synonyms.touched) {
                <p class="text-xs text-red-600 mt-1" role="alert">At least one synonym is required.</p>
              }
              <p class="text-xs text-muted mt-1">Separate multiple synonyms with commas. Terms are stored in lower-case.</p>
            </div>

            <!-- API error -->
            @if (saveError()) {
              <p class="text-xs text-red-600" role="alert">{{ saveError() }}</p>
            }

            <!-- Actions -->
            <div class="flex gap-3 pt-1">
              <button
                type="submit"
                [disabled]="saving()"
                class="px-4 py-2 bg-navy text-white text-sm font-medium rounded
                       hover:bg-navy/90 disabled:opacity-50 disabled:cursor-not-allowed
                       transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
              >
                @if (saving()) { Saving… } @else { Save }
              </button>
              <button
                type="button"
                (click)="closeForm()"
                class="px-4 py-2 border border-border text-sm font-medium rounded text-dark
                       hover:bg-gray-50 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-300"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      }

      <!-- Global error / loading -->
      @if (loadError()) {
        <div class="bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm text-red-700" role="alert">
          Failed to load synonyms: {{ loadError() }}
        </div>
      }

      <!-- Synonyms table -->
      <div class="bg-white rounded-xl border border-border shadow-sm overflow-hidden">
        <div class="px-5 py-4 border-b border-border flex items-center justify-between">
          <h2 class="text-sm font-semibold text-dark">
            Synonym Dictionary
            @if (!loading() && synonyms().length > 0) {
              <span class="text-muted font-normal ml-1">({{ synonyms().length }} entries)</span>
            }
          </h2>
        </div>

        @if (loading()) {
          <div class="p-10 text-center text-muted text-sm animate-pulse">Loading synonyms…</div>
        } @else if (synonyms().length === 0) {
          <div class="p-10 text-center">
            <p class="text-3xl mb-2" aria-hidden="true">🔍</p>
            <p class="text-sm font-medium text-dark">No synonyms defined yet.</p>
            <p class="text-xs text-muted mt-1">Add a synonym entry above to improve search relevance.</p>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm" aria-label="Search synonym entries">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Term</th>
                  <th class="px-5 py-3 text-left">Synonyms</th>
                  <th class="px-5 py-3 text-left">Last Updated</th>
                  <th class="px-5 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (entry of synonyms(); track entry.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3">
                      <span class="font-mono text-xs bg-bg px-2 py-0.5 rounded text-navy font-semibold">
                        {{ entry.term }}
                      </span>
                    </td>
                    <td class="px-5 py-3 text-dark/80 text-xs">
                      <div class="flex flex-wrap gap-1 max-w-xs">
                        @for (syn of entry.synonyms; track syn) {
                          <span class="inline-block bg-gray-100 text-dark px-2 py-0.5 rounded-full text-[11px]">
                            {{ syn }}
                          </span>
                        }
                      </div>
                    </td>
                    <td class="px-5 py-3 text-xs text-muted whitespace-nowrap">
                      {{ entry.updatedAt | date:'mediumDate' }}
                    </td>
                    <td class="px-5 py-3 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          [attr.aria-label]="'Edit synonym ' + entry.term"
                          class="text-xs text-navy hover:text-navy/70 font-medium transition
                                 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-navy rounded"
                          (click)="openEdit(entry)"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          [attr.aria-label]="'Delete synonym ' + entry.term"
                          [disabled]="deletingTerm() === entry.term"
                          class="text-xs text-red hover:text-red/70 font-medium transition
                                 disabled:opacity-40 disabled:cursor-not-allowed
                                 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-red rounded"
                          (click)="onDelete(entry.term)"
                        >
                          @if (deletingTerm() === entry.term) { Deleting… } @else { Delete }
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  `,
})
export class SearchSynonymsComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly fb  = inject(NonNullableFormBuilder);

  readonly synonyms    = signal<SearchSynonymDto[]>([]);
  readonly loading     = signal(true);
  readonly loadError   = signal<string | null>(null);
  readonly showForm    = signal(false);
  readonly editingTerm = signal<string | null>(null);
  readonly saving      = signal(false);
  readonly saveError   = signal<string | null>(null);
  readonly deletingTerm = signal<string | null>(null);

  readonly form = this.fb.group({
    term:     this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    synonyms: this.fb.control('', Validators.required),
  });

  ngOnInit(): void {
    this.loadSynonyms();
  }

  private loadSynonyms(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.api.getSynonyms().pipe(
      catchError((err: unknown) => {
        this.loadError.set(err instanceof Error ? err.message : 'Unknown error');
        return of([] as SearchSynonymDto[]);
      }),
    ).subscribe((items) => {
      this.synonyms.set(items);
      this.loading.set(false);
    });
  }

  openAdd(): void {
    this.editingTerm.set(null);
    this.form.reset({ term: '', synonyms: '' });
    this.form.controls.term.enable();
    this.saveError.set(null);
    this.showForm.set(true);
  }

  openEdit(entry: SearchSynonymDto): void {
    this.editingTerm.set(entry.term);
    this.form.setValue({
      term:     entry.term,
      synonyms: entry.synonyms.join(', '),
    });
    this.form.controls.term.disable();
    this.saveError.set(null);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingTerm.set(null);
    this.form.controls.term.enable();
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const term     = (this.editingTerm() ?? this.form.controls.term.value).trim().toLowerCase();
    const rawSyns  = this.form.controls.synonyms.value;
    const synonyms = rawSyns
      .split(',')
      .map((s) => s.trim().toLowerCase())
      .filter((s) => s.length > 0);

    if (synonyms.length === 0) {
      this.saveError.set('Please provide at least one synonym.');
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);

    this.api.upsertSynonym({ term, synonyms }).pipe(
      catchError((err: unknown) => {
        this.saveError.set(err instanceof Error ? err.message : 'Save failed.');
        this.saving.set(false);
        return of(null);
      }),
    ).subscribe((result) => {
      if (!result) return;
      this.saving.set(false);
      this.closeForm();
      // Merge into local list without a full reload
      this.synonyms.update((list) => {
        const idx = list.findIndex((e) => e.term === result.term);
        return idx >= 0
          ? list.map((e) => (e.term === result.term ? result : e))
          : [result, ...list];
      });
    });
  }

  onDelete(term: string): void {
    if (!confirm(`Delete synonym for "${term}"? This cannot be undone.`)) return;
    this.deletingTerm.set(term);
    this.api.deleteSynonym(term).pipe(
      catchError(() => of(null)),
    ).subscribe(() => {
      this.deletingTerm.set(null);
      this.synonyms.update((list) => list.filter((e) => e.term !== term));
    });
  }
}
