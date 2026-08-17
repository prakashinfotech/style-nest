import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Store } from '@ngrx/store';
import { selectCartCount } from '../store/cart/cart.selectors';
import { selectIsLoggedIn } from '../store/auth/auth.selectors';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, AsyncPipe, RouterLink, RouterLinkActive],
  template: `
    <!-- Mobile-only bottom nav (hidden md+) -->
    <nav class="md:hidden fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 z-50 safe-area-bottom">
      <div class="flex justify-around items-center h-16">

        <!-- Home -->
        <a routerLink="/" routerLinkActive="text-navy" [routerLinkActiveOptions]="{ exact: true }"
           class="flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
          </svg>
          Home
        </a>

        <!-- Shop -->
        <a routerLink="/products" routerLinkActive="text-navy"
           class="flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"/>
          </svg>
          Shop
        </a>

        <!-- Cart -->
        <a routerLink="/cart" routerLinkActive="text-navy"
           class="relative flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
          </svg>
          @if ((cartCount$ | async) ?? 0; as count) {
            @if (count > 0) {
              <span class="absolute top-0 right-3 bg-red text-white text-[9px] font-bold rounded-full w-4 h-4 flex items-center justify-center">
                {{ count > 9 ? '9+' : count }}
              </span>
            }
          }
          Bag
        </a>

        <!-- Wishlist -->
        <a routerLink="/account/wishlist" routerLinkActive="text-navy"
           class="flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
          </svg>
          Wishlist
        </a>

        <!-- Account -->
        @if (isLoggedIn$ | async) {
          <a routerLink="/account" routerLinkActive="text-navy"
             class="flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
            </svg>
            Account
          </a>
        } @else {
          <a routerLink="/auth/login" routerLinkActive="text-navy"
             class="flex flex-col items-center gap-0.5 text-muted text-[10px] pt-2 flex-1">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"/>
            </svg>
            Sign In
          </a>
        }
      </div>
    </nav>
    <!-- Spacer so content isn't hidden behind bottom nav on mobile -->
    <div class="md:hidden h-16"></div>
  `,
})
export class BottomNavComponent {
  private readonly store = inject(Store);
  readonly cartCount$  = this.store.select(selectCartCount);
  readonly isLoggedIn$ = this.store.select(selectIsLoggedIn);
}
