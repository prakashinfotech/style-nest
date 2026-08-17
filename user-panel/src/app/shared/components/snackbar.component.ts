import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe, NgClass } from '@angular/common';
import { Store } from '@ngrx/store';
import { selectSnackbar } from '../../store/ui/ui.selectors';
import { UiActions } from '../../store/ui/ui.actions';

@Component({
  selector: 'app-snackbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, NgClass],
  template: `
    @if (snackbar$ | async; as sb) {
      @if (sb.visible) {
        <div
          role="alert"
          aria-live="assertive"
          class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex items-center gap-3 px-5 py-3 rounded-lg shadow-xl text-white text-sm font-medium min-w-[260px] max-w-sm"
          [ngClass]="{
            'bg-[#2E7D32]': sb.type === 'success',
            'bg-[#E31837]': sb.type === 'error',
            'bg-[#0071C2]': sb.type === 'info',
            'bg-[#C9A84C]': sb.type === 'warning'
          }"
        >
          <span class="flex-1">{{ sb.message }}</span>
          <button
            class="text-white/70 hover:text-white leading-none flex-shrink-0"
            aria-label="Dismiss"
            (click)="dismiss()"
          >✕</button>
        </div>
      }
    }
  `,
})
export class SnackbarComponent {
  private readonly store = inject(Store);
  readonly snackbar$ = this.store.select(selectSnackbar);

  dismiss(): void {
    this.store.dispatch(UiActions.hideSnackbar());
  }
}
