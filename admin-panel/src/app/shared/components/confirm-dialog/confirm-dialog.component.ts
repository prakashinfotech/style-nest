import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (open) {
      <!-- Backdrop -->
      <div
        class="fixed inset-0 bg-black/50 z-40 flex items-center justify-center p-4"
        (click)="onBackdrop($event)"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="title"
      >
        <!-- Panel -->
        <div
          class="bg-white rounded-xl shadow-xl w-full max-w-md p-6 relative z-50"
          (click)="$event.stopPropagation()"
        >
          <!-- Icon -->
          @if (variant === 'danger') {
            <div class="w-12 h-12 rounded-full bg-red/10 flex items-center justify-center mx-auto mb-4">
              <span class="text-red text-2xl" aria-hidden="true">⚠</span>
            </div>
          }

          <h3 class="text-lg font-semibold text-dark text-center">{{ title }}</h3>
          <p class="text-sm text-muted text-center mt-2">{{ message }}</p>

          <div class="flex gap-3 mt-6">
            <button
              type="button"
              class="flex-1 px-4 py-2 rounded-lg border border-border text-dark text-sm font-medium hover:bg-bg transition-colors"
              (click)="cancel.emit()"
            >
              {{ cancelLabel }}
            </button>
            <button
              type="button"
              class="flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-colors"
              [class]="variant === 'danger'
                ? 'bg-red text-white hover:bg-red/90'
                : 'bg-navy text-white hover:bg-navy/90'"
              (click)="confirm.emit()"
            >
              {{ confirmLabel }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmDialogComponent {
  @Input() open = false;
  @Input() title = 'Confirm';
  @Input() message = 'Are you sure you want to proceed?';
  @Input() confirmLabel = 'Confirm';
  @Input() cancelLabel = 'Cancel';
  @Input() variant: 'default' | 'danger' = 'danger';

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onBackdrop(event: MouseEvent): void {
    if ((event.target as HTMLElement) === event.currentTarget) {
      this.cancel.emit();
    }
  }
}
