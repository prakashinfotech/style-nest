import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { selectIsLoggedIn } from '../../store/auth/auth.selectors';
import { UserService, Notification } from '../../core/services/user.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NgClass],
  template: `
    @if (isLoggedIn()) {
      <div class="relative" #bellWrapper>
        <button
          type="button"
          (click)="toggleDropdown()"
          class="hidden md:flex flex-col items-center gap-0.5 px-2 py-1 rounded-md hover:bg-bg transition min-h-[44px] min-w-[44px] justify-center relative"
          aria-label="Notifications"
        >
          <div class="relative">
            <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
            </svg>
            @if (unreadCount() > 0) {
              <span class="absolute -top-1.5 -right-1.5 bg-red text-white text-[10px] font-bold rounded-full w-4 h-4 flex items-center justify-center">
                {{ unreadCount() > 9 ? '9+' : unreadCount() }}
              </span>
            }
          </div>
          <span class="text-[11px] tracking-widest text-dark uppercase">Alerts</span>
        </button>

        <!-- Dropdown -->
        @if (open()) {
          <div class="absolute top-full right-0 mt-2 w-80 bg-white rounded-xl border border-border shadow-lg z-50 overflow-hidden">
            <div class="flex items-center justify-between px-4 py-3 border-b border-border">
              <h3 class="text-sm font-semibold text-dark">Notifications</h3>
              @if (unreadCount() > 0) {
                <button type="button" (click)="markAllRead()"
                        class="text-xs text-red hover:underline">Mark all read</button>
              }
            </div>

            <div class="max-h-80 overflow-y-auto">
              @if (loading()) {
                <div class="p-4 text-center text-sm text-muted">Loading...</div>
              } @else if (notifications().length === 0) {
                <div class="py-10 text-center">
                  <svg class="w-8 h-8 text-border mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
                  </svg>
                  <p class="text-sm text-muted">You're all caught up!</p>
                </div>
              } @else {
                @for (n of notifications(); track n.id) {
                  <div (click)="markRead(n)"
                       class="px-4 py-3 border-b border-border last:border-0 cursor-pointer transition-colors"
                       [ngClass]="n.isRead ? 'hover:bg-bg/50' : 'bg-red/5 hover:bg-red/10'">
                    <div class="flex items-start gap-3">
                      <div class="w-2 h-2 rounded-full mt-1.5 shrink-0"
                           [ngClass]="n.isRead ? 'bg-border' : 'bg-red'"></div>
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-medium text-dark line-clamp-1">{{ n.title }}</p>
                        <p class="text-xs text-muted mt-0.5 line-clamp-2">{{ n.body }}</p>
                        <p class="text-[11px] text-mid-gray mt-1">{{ n.createdAt | date:'shortDate' }}</p>
                      </div>
                    </div>
                  </div>
                }
              }
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class NotificationBellComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  readonly isLoggedIn = this.store.selectSignal(selectIsLoggedIn);
  readonly notifications = signal<Notification[]>([]);
  readonly unreadCount = signal(0);
  readonly loading = signal(false);
  readonly open = signal(false);

  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.loadUnreadCount();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('app-notification-bell')) {
      this.open.set(false);
    }
  }

  toggleDropdown(): void {
    const next = !this.open();
    this.open.set(next);
    if (next && this.notifications().length === 0) {
      this.loadNotifications();
    }
  }

  markRead(n: Notification): void {
    if (!n.isRead) {
      this.userService.markNotificationRead(n.id).subscribe(() => {
        this.notifications.update((list) =>
          list.map((item) => (item.id === n.id ? { ...item, isRead: true } : item)),
        );
        this.unreadCount.update((c) => Math.max(0, c - 1));
      });
    }
  }

  markAllRead(): void {
    this.userService.markAllNotificationsRead().subscribe(() => {
      this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      this.unreadCount.set(0);
    });
  }

  private loadNotifications(): void {
    this.loading.set(true);
    this.userService.getNotifications().subscribe({
      next: (n) => { this.notifications.set(n); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }

  private loadUnreadCount(): void {
    this.userService.getUnreadNotificationCount().subscribe({
      next: (c) => this.unreadCount.set(c),
      error: () => {},
    });
  }
}
