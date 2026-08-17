import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { selectCurrentUser } from '../../store/auth/auth.selectors';
import { logout } from '../../store/auth/auth.actions';

@Component({
  selector: 'app-topbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe],
  template: `
    <header class="h-14 bg-white border-b border-border flex items-center justify-between px-6 flex-shrink-0">
      <div class="text-sm text-muted">
        StyleNest Administration
      </div>
      @if (user$ | async; as user) {
        <div class="flex items-center gap-4">
          <span class="text-sm font-medium text-dark">{{ user.firstName }} {{ user.lastName }}</span>
          <span class="text-xs bg-navy/10 text-navy px-2 py-0.5 rounded-full font-medium">{{ user.roles[0] }}</span>
          <button
            (click)="signOut()"
            class="text-sm text-muted hover:text-red transition-colors">
            Sign out
          </button>
        </div>
      }
    </header>
  `,
})
export class TopbarComponent {
  private readonly store = inject(Store);
  readonly user$ = this.store.select(selectCurrentUser);

  signOut(): void {
    this.store.dispatch(logout());
  }
}
