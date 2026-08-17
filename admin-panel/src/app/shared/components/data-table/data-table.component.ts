import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { NgClass } from '@angular/common';

export interface TableColumn<T = Record<string, unknown>> {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  class?: string;
  render?: (row: T) => string;
}

export interface TableAction<T = Record<string, unknown>> {
  label: string;
  class?: string;
  disabled?: (row: T) => boolean;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
      <!-- Header slot -->
      @if (title) {
        <div class="px-5 py-4 border-b border-border flex items-center justify-between">
          <h2 class="font-semibold text-dark text-sm">{{ title }}</h2>
          <ng-content select="[table-header-actions]" />
        </div>
      }

      <!-- Table -->
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
              @for (col of columns; track col.key) {
                <th
                  class="px-5 py-3"
                  [ngClass]="{
                    'text-left': col.align !== 'center' && col.align !== 'right',
                    'text-center': col.align === 'center',
                    'text-right': col.align === 'right'
                  }"
                >
                  {{ col.label }}
                </th>
              }
              @if (actions.length) {
                <th class="px-5 py-3 text-right">Actions</th>
              }
            </tr>
          </thead>
          <tbody>
            @if (loading) {
              @for (i of skeletonRows; track i) {
                <tr class="border-b border-border/50">
                  @for (col of columns; track col.key) {
                    <td class="px-5 py-3">
                      <div class="h-4 bg-bg rounded animate-pulse"></div>
                    </td>
                  }
                  @if (actions.length) {
                    <td class="px-5 py-3 text-right">
                      <div class="h-4 w-24 bg-bg rounded animate-pulse ml-auto"></div>
                    </td>
                  }
                </tr>
              }
            } @else if (!rows.length) {
              <tr>
                <td [attr.colspan]="columns.length + (actions.length ? 1 : 0)" class="px-5 py-12 text-center text-muted">
                  {{ emptyMessage }}
                </td>
              </tr>
            } @else {
              @for (row of rows; track rowKey(row)) {
                <tr class="border-b border-border/50 hover:bg-bg/40 transition-colors">
                  @for (col of columns; track col.key) {
                    <td
                      class="px-5 py-3 text-dark"
                      [ngClass]="{
                        'text-left': col.align !== 'center' && col.align !== 'right',
                        'text-center': col.align === 'center',
                        'text-right': col.align === 'right',
                        'font-medium': col.key === primaryKey
                      }"
                    >
                      {{ col.render ? col.render(row) : getCellValue(row, col.key) }}
                    </td>
                  }
                  @if (actions.length) {
                    <td class="px-5 py-3 text-right space-x-2">
                      @for (action of actions; track action.label) {
                        <button
                          type="button"
                          class="text-xs font-medium px-3 py-1 rounded border transition-colors"
                          [class]="action.class ?? 'border-border text-dark hover:bg-bg'"
                          [disabled]="action.disabled ? action.disabled(row) : false"
                          (click)="actionClick.emit({ action: action.label, row })"
                        >
                          {{ action.label }}
                        </button>
                      }
                    </td>
                  }
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      @if (totalPages > 1) {
        <div class="px-5 py-3 border-t border-border flex items-center justify-between text-sm">
          <span class="text-muted">Page {{ currentPage }} of {{ totalPages }}</span>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40 transition-colors"
              [disabled]="currentPage <= 1"
              (click)="pageChange.emit(currentPage - 1)"
            >
              Prev
            </button>
            <button
              type="button"
              class="px-3 py-1 rounded border border-border text-dark hover:bg-bg disabled:opacity-40 transition-colors"
              [disabled]="currentPage >= totalPages"
              (click)="pageChange.emit(currentPage + 1)"
            >
              Next
            </button>
          </div>
        </div>
      }
    </div>
  `,
})
export class DataTableComponent<T extends Record<string, unknown> = Record<string, unknown>> {
  @Input() title = '';
  @Input() columns: TableColumn<T>[] = [];
  @Input() rows: T[] = [];
  @Input() actions: TableAction<T>[] = [];
  @Input() loading = false;
  @Input() emptyMessage = 'No records found.';
  @Input() primaryKey = 'id';
  @Input() currentPage = 1;
  @Input() totalPages = 1;

  @Output() actionClick = new EventEmitter<{ action: string; row: T }>();
  @Output() pageChange = new EventEmitter<number>();

  readonly skeletonRows = [1, 2, 3, 4, 5];

  rowKey(row: T): unknown {
    return row[this.primaryKey] ?? JSON.stringify(row);
  }

  getCellValue(row: T, key: string): string {
    const val = row[key];
    return val !== null && val !== undefined ? String(val) : '—';
  }
}
