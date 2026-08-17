import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';

interface BrandDto { id: string; name: string; slug: string; logoUrl: string | null; }

@Component({
  selector: 'app-brands',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Brand Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">All Brands</h2>
        </div>
        @if (brands$ | async; as brands) {
          <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 p-5">
            @for (b of brands; track b.id) {
              <div class="border border-border rounded-lg p-4 flex flex-col items-center gap-2 hover:shadow-sm transition-shadow">
                @if (b.logoUrl) {
                  <img [src]="b.logoUrl" [alt]="b.name" class="w-12 h-12 object-contain">
                } @else {
                  <div class="w-12 h-12 bg-bg rounded-full flex items-center justify-center text-mid-gray font-bold text-lg">
                    {{ b.name[0] }}
                  </div>
                }
                <span class="text-sm font-medium text-dark text-center">{{ b.name }}</span>
              </div>
            }
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading brands…</div>
        }
      </div>
    </div>
  `,
})
export class BrandsComponent implements OnInit {
  brands$: Observable<BrandDto[]> | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.brands$ = this.http.get<BrandDto[]>('/api/v1/brands').pipe(catchError(() => of([])));
  }
}
