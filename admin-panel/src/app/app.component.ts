import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { AsyncPipe } from '@angular/common';
import { selectToast } from './store/ui/ui.selectors';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AsyncPipe],
  template: `
    <router-outlet />

    @if (toast$ | async; as toast) {
      @if (toast.message) {
        <div
          class="fixed bottom-6 right-6 z-50 px-5 py-3 rounded-lg shadow-lg text-white font-medium text-sm"
          [class.bg-success]="toast.type === 'success'"
          [class.bg-error]="toast.type === 'error'"
          [class.bg-navy]="toast.type === 'info'">
          {{ toast.message }}
        </div>
      }
    }
  `,
})
export class AppComponent {
  private readonly store = inject(Store);
  readonly toast$ = this.store.select(selectToast);
}
