import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';

interface SizeRow {
  size: string;
  chest: string;
  waist: string;
  hips: string;
}

@Component({
  selector: 'app-size-guide-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- Backdrop -->
    <div
      class="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/50 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="size-guide-title"
      (click)="close.emit()"
      (keydown.Escape)="close.emit()"
    >
      <!-- Panel — stop propagation so clicking inside doesn't close -->
      <div
        class="bg-white rounded-t-2xl sm:rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto
               shadow-2xl"
        (click)="$event.stopPropagation()"
      >
        <!-- Header -->
        <div class="flex items-center justify-between px-5 py-4 border-b border-border sticky top-0 bg-white z-10">
          <h2 id="size-guide-title" class="text-base font-semibold text-dark">Size Guide</h2>
          <button
            class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-gray-100 transition
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red"
            aria-label="Close size guide"
            (click)="close.emit()"
          >
            <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Content -->
        <div class="px-5 py-4 space-y-5">
          <!-- How to measure tip -->
          <div class="bg-blue-50 border border-blue-100 rounded-lg p-3 text-xs text-blue-800">
            <strong>How to measure:</strong> Use a soft measuring tape. Keep it snug but not tight.
            Measurements are in centimetres (cm).
          </div>

          <!-- Women's sizes -->
          <div>
            <h3 class="text-sm font-semibold text-dark mb-2">Women's Sizes</h3>
            <div class="overflow-x-auto">
              <table class="w-full text-xs border-collapse" aria-label="Women's size chart">
                <thead>
                  <tr class="bg-gray-50">
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Size</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Chest (cm)</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Waist (cm)</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Hips (cm)</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of womenSizes; track row.size) {
                    <tr class="hover:bg-gray-50 transition-colors">
                      <td class="border border-border px-3 py-2 font-medium text-dark">{{ row.size }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.chest }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.waist }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.hips }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>

          <!-- Men's sizes -->
          <div>
            <h3 class="text-sm font-semibold text-dark mb-2">Men's Sizes</h3>
            <div class="overflow-x-auto">
              <table class="w-full text-xs border-collapse" aria-label="Men's size chart">
                <thead>
                  <tr class="bg-gray-50">
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Size</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Chest (cm)</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Waist (cm)</th>
                    <th class="border border-border px-3 py-2 text-left font-semibold text-dark">Hips (cm)</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of menSizes; track row.size) {
                    <tr class="hover:bg-gray-50 transition-colors">
                      <td class="border border-border px-3 py-2 font-medium text-dark">{{ row.size }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.chest }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.waist }}</td>
                      <td class="border border-border px-3 py-2 text-muted">{{ row.hips }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>

          <!-- Fit tip -->
          <p class="text-xs text-muted">
            If you're between sizes, we recommend sizing up for a more comfortable fit.
            All measurements are approximate and may vary slightly by style.
          </p>
        </div>
      </div>
    </div>
  `,
})
export class SizeGuideModalComponent {
  @Output() close = new EventEmitter<void>();

  readonly womenSizes: SizeRow[] = [
    { size: 'XS', chest: '76–81',  waist: '58–63',  hips: '84–89'  },
    { size: 'S',  chest: '81–86',  waist: '63–68',  hips: '89–94'  },
    { size: 'M',  chest: '86–91',  waist: '68–73',  hips: '94–99'  },
    { size: 'L',  chest: '91–96',  waist: '73–78',  hips: '99–104' },
    { size: 'XL', chest: '96–101', waist: '78–83',  hips: '104–109'},
    { size: 'XXL',chest: '101–106',waist: '83–88',  hips: '109–114'},
  ];

  readonly menSizes: SizeRow[] = [
    { size: 'XS', chest: '81–86',  waist: '66–71',  hips: '84–89'  },
    { size: 'S',  chest: '86–91',  waist: '71–76',  hips: '89–94'  },
    { size: 'M',  chest: '91–96',  waist: '76–81',  hips: '94–99'  },
    { size: 'L',  chest: '96–101', waist: '81–86',  hips: '99–104' },
    { size: 'XL', chest: '101–106',waist: '86–91',  hips: '104–109'},
    { size: 'XXL',chest: '106–111',waist: '91–96',  hips: '109–114'},
  ];
}
