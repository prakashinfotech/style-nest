import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';

interface CategoryDto { id: string; name: string; slug: string; parentId: string | null; imageUrl: string | null; }

@Component({
  selector: 'app-categories',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Category Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">All Categories</h2>
        </div>
        @if (cats$ | async; as cats) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Name</th>
                  <th class="px-5 py-3 text-left">Slug</th>
                  <th class="px-5 py-3 text-center">Parent</th>
                </tr>
              </thead>
              <tbody>
                @for (c of cats; track c.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ c.name }}</td>
                    <td class="px-5 py-3 font-mono text-xs text-muted">{{ c.slug }}</td>
                    <td class="px-5 py-3 text-center text-xs text-muted">{{ c.parentId ? '↳ sub' : 'Root' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading categories…</div>
        }
      </div>
    </div>
  `,
})
export class CategoriesComponent implements OnInit {
  cats$: Observable<CategoryDto[]> | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cats$ = this.http.get<CategoryDto[]>('/api/v1/categories').pipe(catchError(() => of([])));
  }
}
