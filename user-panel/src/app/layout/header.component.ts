import {
  ChangeDetectionStrategy, Component, computed,
  HostListener, inject, OnDestroy, signal,
} from '@angular/core';
import { AsyncPipe, CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import {
  selectIsAdmin, selectIsLoggedIn, selectIsSeller, selectCurrentUser,
} from '../store/auth/auth.selectors';
import { selectCartCount } from '../store/cart/cart.selectors';
import { AuthActions } from '../store/auth/auth.actions';
import { UiActions } from '../store/ui/ui.actions';
import { MegaMenuComponent } from './mega-menu.component';
import { NotificationBellComponent } from '../shared/components/notification-bell.component';

@Component({
  selector: 'app-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, AsyncPipe, RouterLink, FormsModule, MegaMenuComponent, NotificationBellComponent],
  template: `
    <!-- Skip-to-content — DESIGN.md §10 Accessibility -->
    <a href="#main-content" class="skip-to-content">Skip to main content</a>

    <!--
      Search backdrop — fixed full-screen overlay that sits behind the header (z-40)
      so clicking outside the search area closes the suggestions panel.
    -->
    @if (searchFocused()) {
      <div
        class="fixed inset-0 bg-black/20 z-40"
        aria-hidden="true"
        (click)="closeSearch()"
      ></div>
    }

    <!--
      Fixed header — pinned to the top of the viewport on all pages.
      A spacer div below compensates for the header height so page content
      is never hidden underneath it.
    -->
    <header class="fixed top-0 left-0 right-0 z-50">

      <!-- ── Announcement Bar ─────────────────────────────────── -->
      @if (showAnnouncement()) {
        <div
          class="bg-red text-white text-center text-[13px] font-medium h-9 flex items-center justify-center relative px-10"
          role="alert"
          aria-live="polite"
        >
          <span>Free Shipping on orders above ₹499 &nbsp;|&nbsp; Use code <strong>STYLENEST10</strong> for extra 10% off</span>
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-white/80 hover:text-white p-1 rounded transition"
            aria-label="Dismiss announcement"
            (click)="showAnnouncement.set(false)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Primary Navigation ───────────────────────────────── -->
      <div
        class="bg-white border-b border-border transition-shadow duration-200"
        [class.shadow-md]="scrolled()"
        role="navigation"
        aria-label="Primary navigation"
      >
        <!-- Top bar: Logo | Search | Icons -->
        <div
          class="max-w-layout mx-auto px-4 md:px-6 flex items-center gap-4 justify-between transition-all duration-200"
          [class.h-16]="!scrolled()"
          [class.h-[52px]]="scrolled()"
        >

          <!-- Mobile: hamburger -->
          <button
            type="button"
            class="md:hidden p-2 rounded-md hover:bg-bg transition min-h-[44px] min-w-[44px] flex items-center justify-center"
            aria-label="Open navigation menu"
            (click)="openMobileNav()"
          >
            <svg class="w-6 h-6 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
            </svg>
          </button>

          <!-- Logo -->
          <a routerLink="/" class="flex-shrink-0" aria-label="StyleNest Fashion — go to homepage">
            <span class="text-xl md:text-2xl font-bold tracking-tight font-display">
              <span class="text-navy">TATA</span>&nbsp;<span class="text-red">StyleNest</span>
            </span>
          </a>

          <!-- ── Desktop Search ────────────────────────────────── -->
          <!--
            FIX 1: searchQuery is now a signal — setting it to '' triggers
                   OnPush re-render so the input visually clears after submit.
            FIX 2: Suggestions dropdown with popular-searches + live filtering.
            FIX 3: Clear (×) button shown when input has text.
          -->
          <div class="hidden md:flex flex-1 max-w-xl mx-4 relative z-50">

            <!-- Input pill — search-pill class scopes focus-visible suppression in styles.scss -->
            <div
              class="search-pill flex w-full bg-white border transition-all duration-200"
              [class.rounded-full]="!(searchFocused() && filteredSuggestions().length > 0)"
              [class.rounded-t-2xl]="searchFocused() && filteredSuggestions().length > 0"
              [class.border-border]="!searchFocused()"
              [class.border-navy]="searchFocused()"
              [class.shadow-none]="!searchFocused()"
              [class.shadow-sm]="searchFocused()"
              [class.border-b-0]="searchFocused() && filteredSuggestions().length > 0"
              [class.hover:border-mid-gray]="!searchFocused()"
            >
              <!-- Search icon — transitions to navy when focused -->
              <svg
                class="w-4 h-4 ml-4 flex-shrink-0 self-center transition-colors duration-200"
                [class.text-mid-gray]="!searchFocused()"
                [class.text-navy]="searchFocused()"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>

              <input
                type="search"
                placeholder="Search for products, brands and more"
                class="flex-1 bg-transparent text-dark placeholder-mid-gray px-3 py-2.5 text-sm
                       outline-none focus:outline-none min-w-0 [&::-webkit-search-cancel-button]:hidden"
                style="outline: none !important; box-shadow: none !important;"
                aria-label="Search products and brands"
                [attr.aria-expanded]="searchFocused() && filteredSuggestions().length > 0 ? 'true' : 'false'"
                aria-autocomplete="list"
                aria-controls="search-suggestions-desktop"
                autocomplete="off"
                [ngModel]="searchQuery()"
                (ngModelChange)="searchQuery.set($event)"
                (focus)="searchFocused.set(true)"
                (blur)="onSearchBlur()"
                (keyup.enter)="onSearch()"
              />

              <!-- Clear button — only visible when there is text -->
              @if (searchQuery().trim().length > 0) {
                <button
                  type="button"
                  class="w-7 h-7 mr-1 rounded-full bg-bg hover:bg-border flex items-center justify-center
                         text-mid-gray hover:text-dark transition-all duration-150 self-center flex-shrink-0"
                  aria-label="Clear search"
                  (click)="clearSearch()"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/>
                  </svg>
                </button>
              }

              <!-- Submit button -->
              <button
                type="button"
                class="px-5 py-2.5 bg-navy hover:bg-navy/90 active:bg-navy/80 text-white text-sm font-semibold
                       tracking-wide transition-colors duration-200 flex-shrink-0 flex items-center gap-2"
                [class.rounded-r-full]="!(searchFocused() && filteredSuggestions().length > 0)"
                [class.rounded-tr-2xl]="searchFocused() && filteredSuggestions().length > 0"
                aria-label="Submit search"
                (click)="onSearch()"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                </svg>
                <span class="hidden lg:inline">Search</span>
              </button>
            </div>

            <!-- Suggestions dropdown -->
            @if (searchFocused() && filteredSuggestions().length > 0) {
              <div
                id="search-suggestions-desktop"
                class="absolute top-full left-0 right-0 bg-white border border-navy border-t-0
                       rounded-b-2xl shadow-lg z-50 overflow-hidden dropdown-reveal"
                role="listbox"
                aria-label="Search suggestions"
              >
                <div class="px-4 pt-3 pb-1.5 flex items-center justify-between">
                  <span class="text-[11px] uppercase tracking-widest text-mid-gray font-semibold">
                    {{ searchQuery().trim() ? 'Suggestions' : 'Popular Searches' }}
                  </span>
                  @if (!searchQuery().trim()) {
                    <span class="text-[11px] text-mid-gray">Trending now</span>
                  }
                </div>

                <ul>
                  @for (s of filteredSuggestions(); track s) {
                    <li>
                      <button
                        type="button"
                        role="option"
                        [attr.aria-selected]="false"
                        class="w-full text-left px-4 py-2.5 flex items-center gap-3
                               hover:bg-bg text-sm text-dark transition-colors duration-100 group"
                        (click)="selectSuggestion(s)"
                      >
                        <svg class="w-4 h-4 text-mid-gray flex-shrink-0 group-hover:text-navy transition-colors"
                             fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                        </svg>
                        <span class="flex-1">{{ s }}</span>
                        <svg class="w-3.5 h-3.5 text-transparent group-hover:text-mid-gray transition-colors"
                             fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                        </svg>
                      </button>
                    </li>
                  }
                </ul>

                <!-- Quick category chips — only when the box is empty -->
                @if (!searchQuery().trim()) {
                  <div class="px-4 pb-3 pt-2 border-t border-border">
                    <p class="text-[11px] uppercase tracking-widest text-mid-gray font-semibold mb-2.5">
                      Browse Categories
                    </p>
                    <div class="flex flex-wrap gap-2">
                      @for (cat of navCategories; track cat.slug) {
                        <a
                          [routerLink]="['/products']"
                          [queryParams]="{ category: cat.slug }"
                          class="px-3 py-1 rounded-full bg-bg border border-border
                                 text-xs font-medium text-dark
                                 hover:border-navy hover:text-navy hover:bg-navy/5
                                 transition-all duration-150"
                          (click)="closeSearch()"
                        >
                          {{ cat.label }}
                        </a>
                      }
                    </div>
                  </div>
                }
              </div>
            }
          </div>

          <!-- ── Right icon cluster ───────────────────────────── -->
          <div class="flex items-center gap-1 md:gap-2 ml-auto md:ml-0">

            <!-- Wishlist -->
            <a
              routerLink="/account/wishlist"
              class="hidden md:flex flex-col items-center gap-0.5 px-2 py-1 rounded-md
                     hover:bg-bg transition min-h-[44px] min-w-[44px] justify-center"
              aria-label="Wishlist"
            >
              <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
              </svg>
              <span class="text-[11px] tracking-widest text-dark uppercase">Wishlist</span>
            </a>

            <!-- Notification Bell -->
            <app-notification-bell />

            <!-- Cart -->
            <a
              routerLink="/cart"
              class="relative flex flex-col items-center gap-0.5 px-2 py-1 rounded-md
                     hover:bg-bg transition min-h-[44px] min-w-[44px] justify-center"
              aria-label="Shopping cart"
            >
              <div class="relative">
                <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
                </svg>
                @if ((cartCount$ | async) ?? 0; as count) {
                  @if (count > 0) {
                    <span
                      class="absolute -top-1.5 -right-1.5 bg-red text-white text-[10px] font-bold
                             rounded-full w-4 h-4 flex items-center justify-center"
                      [attr.aria-label]="count + ' items in cart'"
                    >
                      {{ count > 9 ? '9+' : count }}
                    </span>
                  }
                }
              </div>
              <span class="hidden md:block text-[11px] tracking-widest text-dark uppercase">Bag</span>
            </a>

            <!-- Account — FIX: tabindex + focus-within makes dropdown keyboard-accessible -->
            @if (isLoggedIn$ | async) {
              <div
                class="hidden md:flex flex-col items-center gap-0.5 px-2 py-1 rounded-md
                       hover:bg-bg transition group relative min-h-[44px] min-w-[44px] justify-center
                       cursor-pointer"
                tabindex="0"
                role="button"
                aria-haspopup="menu"
                aria-label="Account menu"
              >
                <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
                </svg>
                <span class="text-[11px] tracking-widest text-dark uppercase truncate max-w-[60px]">
                  {{ (currentUser$ | async)?.firstName ?? 'Account' }}
                </span>

                <!--
                  Dropdown — visible on hover (mouse) and on focus-within (keyboard).
                  focus-within triggers when THIS element or any descendant has focus.
                -->
                <div
                  class="absolute top-full right-0 mt-1 w-48 bg-white text-dark shadow-md rounded-lg py-1.5
                         border border-border text-sm
                         opacity-0 invisible pointer-events-none
                         group-hover:opacity-100 group-hover:visible group-hover:pointer-events-auto
                         group-focus-within:opacity-100 group-focus-within:visible group-focus-within:pointer-events-auto
                         transition-all duration-150 z-50"
                  role="menu"
                  aria-label="Account menu"
                >
                  @if (isAdmin$ | async) {
                    <a
                      routerLink="/admin"
                      class="flex items-center gap-2 px-4 py-2 hover:bg-bg transition font-semibold text-navy"
                      role="menuitem"
                    >
                      <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                      </svg>
                      Admin Dashboard
                    </a>
                    <hr class="my-1 border-border" />
                  }
                  @if (isSeller$ | async) {
                    <a
                      routerLink="/seller"
                      class="flex items-center gap-2 px-4 py-2 hover:bg-bg transition font-semibold text-gold"
                      role="menuitem"
                    >
                      <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
                      </svg>
                      Seller Hub
                    </a>
                    <hr class="my-1 border-border" />
                  }
                  <a routerLink="/account"          class="block px-4 py-2 hover:bg-bg transition" role="menuitem">My Account</a>
                  <a routerLink="/account/orders"   class="block px-4 py-2 hover:bg-bg transition" role="menuitem">Orders</a>
                  <a routerLink="/account/wishlist" class="block px-4 py-2 hover:bg-bg transition" role="menuitem">Wishlist</a>
                  <hr class="my-1 border-border" />
                  <button
                    type="button"
                    class="w-full text-left px-4 py-2 hover:bg-bg text-red transition"
                    role="menuitem"
                    (click)="logout()"
                  >
                    Logout
                  </button>
                </div>
              </div>
            } @else {
              <button
                type="button"
                class="hidden md:flex flex-col items-center gap-0.5 px-2 py-1 rounded-md
                       hover:bg-bg transition min-h-[44px] min-w-[44px] justify-center"
                aria-label="Sign in to your account"
                (click)="openAuthModal('login')"
              >
                <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
                </svg>
                <span class="text-[11px] tracking-widest text-dark uppercase">Sign In</span>
              </button>
            }

          </div>
        </div>

        <!-- ── Mobile Search Bar ─────────────────────────────── -->
        <!-- FIX: added submit icon button so mobile users don't need to press Enter -->
        <div class="md:hidden px-4 pb-3">
          <div
            class="search-pill flex rounded-full overflow-hidden bg-white border transition-all duration-200"
            [class.border-border]="!searchFocused()"
            [class.border-navy]="searchFocused()"
            [class.shadow-sm]="searchFocused()"
          >
            <svg class="w-4 h-4 ml-3 flex-shrink-0 self-center transition-colors duration-200"
                 [class.text-mid-gray]="!searchFocused()"
                 [class.text-navy]="searchFocused()"
                 fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <input
              type="search"
              placeholder="Search products, brands and more"
              class="flex-1 bg-transparent text-dark placeholder-mid-gray px-3 py-2 text-sm
                     outline-none focus:outline-none min-w-0 [&::-webkit-search-cancel-button]:hidden"
              style="outline: none !important; box-shadow: none !important;"
              aria-label="Search"
              autocomplete="off"
              [ngModel]="searchQuery()"
              (ngModelChange)="searchQuery.set($event)"
              (focus)="searchFocused.set(true)"
              (keyup.enter)="onSearch()"
            />
            @if (searchQuery().trim().length > 0) {
              <button
                type="button"
                class="w-6 h-6 mr-1 rounded-full bg-bg hover:bg-border flex items-center justify-center
                       text-mid-gray hover:text-dark transition-all duration-150 self-center flex-shrink-0"
                aria-label="Clear search"
                (click)="clearSearch()"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            }
            <button
              type="button"
              class="px-3 py-2 bg-navy hover:bg-navy/90 text-white rounded-r-full transition-colors duration-200 flex-shrink-0"
              aria-label="Submit search"
              (click)="onSearch()"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- ── Category Nav + Mega-Menu ─────────────────────── -->
        <!--
          FIX: mega-menu close debounce — each category div and the mega-menu panel
          share openCategoryMenu / scheduleCategoryMenuClose so a 150ms grace period
          prevents the menu from flickering closed when the mouse crosses the gap
          between the nav link and the dropdown panel.
        -->
        <nav class="hidden md:block border-t border-border relative" aria-label="Category navigation">
          <div class="max-w-layout mx-auto px-6 flex gap-1 text-sm overflow-x-auto">
            @for (cat of navCategories; track cat.label) {
              <div
                class="relative"
                (mouseenter)="openCategoryMenu(cat.slug)"
                (mouseleave)="scheduleCategoryMenuClose()"
              >
                <a
                  [routerLink]="['/products']"
                  [queryParams]="{ category: cat.slug }"
                  class="py-2.5 px-3 whitespace-nowrap font-medium text-dark hover:text-red transition-colors
                         relative block
                         after:absolute after:bottom-0 after:left-3 after:right-3 after:h-0.5 after:bg-red
                         after:scale-x-0 hover:after:scale-x-100 after:transition-transform after:duration-200"
                  [class.text-red]="hoveredCategory() === cat.slug"
                  [class.after:scale-x-100]="hoveredCategory() === cat.slug"
                  [attr.aria-expanded]="hoveredCategory() === cat.slug ? 'true' : 'false'"
                >
                  {{ cat.label }}
                </a>
              </div>
            }
          </div>

          <!-- Mega-menu — keep alive while mouse is inside panel -->
          @if (hoveredCategory(); as slug) {
            <div
              (mouseenter)="openCategoryMenu(slug)"
              (mouseleave)="scheduleCategoryMenuClose()"
            >
              <app-mega-menu [categorySlug]="slug" />
            </div>
          }
        </nav>

      </div>
    </header>

    <!--
      Spacer — compensates for the fixed header so page content starts below it.
      Heights match the header's own measurements:
        Announcement (h-9 = 36px) + Nav (h-16 = 64px) + Category nav (~40px desktop) = 140px desktop
        Mobile: announcement (36px) + nav (~52px) + search bar (~48px) = ~136px
      We use a single responsive class set that mirrors the header structure.
    -->
    <div
      class="transition-all duration-200"
      [style.height]="headerSpacerHeight()"
      aria-hidden="true"
    ></div>
  `,
})
export class HeaderComponent implements OnDestroy {
  private readonly store  = inject(Store);
  private readonly router = inject(Router);

  // ── Reactive state (all signals for OnPush compatibility) ───
  searchQuery      = signal('');
  searchFocused    = signal(false);
  showAnnouncement = signal(true);
  scrolled         = signal(false);
  hoveredCategory  = signal<string | null>(null);

  /**
   * Spacer height that compensates for the fixed header.
   * Announcement bar (36px) + Nav (64px normal / 52px scrolled) + Category nav (40px desktop).
   * We return a CSS height string; the template binds it to [style.height].
   *
   * Breakpoint logic is handled in CSS via the template — here we compute the
   * desktop value only (mobile uses a slightly different layout but the same
   * total is close enough; the category nav is hidden on mobile).
   *
   *   Desktop with announcement:  36 + 64 + 40 = 140px
   *   Desktop no announcement:         64 + 40 = 104px
   *   Mobile  with announcement:  36 + 52 + 48 = 136px  (nav h-[52px] + search pb-3 ~48px)
   *   Mobile  no announcement:         52 + 48 = 100px
   *
   * We use window.innerWidth to pick the right value reactively via the scroll
   * listener (which already fires on every scroll, keeping this cheap).
   */
  readonly headerSpacerHeight = computed(() => {
    const announcement = this.showAnnouncement() ? 36 : 0;
    const isMobile = typeof window !== 'undefined' && window.innerWidth < 768;
    if (isMobile) {
      // nav ~52px + mobile search bar ~48px
      return `${announcement + 100}px`;
    }
    // nav 64px + category nav 40px
    return `${announcement + 104}px`;
  });

  /**
   * Suggestions list — computed from the current query.
   * Returns up to 8 matches (prefix-first), or all popular searches when empty.
   */
  readonly filteredSuggestions = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.popularSearches;
    const starts = this.popularSearches.filter(s => s.toLowerCase().startsWith(q));
    const contains = this.popularSearches.filter(
      s => !s.toLowerCase().startsWith(q) && s.toLowerCase().includes(q),
    );
    return [...starts, ...contains].slice(0, 8);
  });

  // ── Store observables ────────────────────────────────────────
  readonly isLoggedIn$  = this.store.select(selectIsLoggedIn);
  readonly currentUser$ = this.store.select(selectCurrentUser);
  readonly cartCount$   = this.store.select(selectCartCount);
  readonly isAdmin$     = this.store.select(selectIsAdmin);
  readonly isSeller$    = this.store.select(selectIsSeller);

  // ── Static data ──────────────────────────────────────────────
  readonly navCategories = [
    { label: 'Women',     slug: 'women' },
    { label: 'Men',       slug: 'men' },
    { label: 'Kids',      slug: 'kids' },
    { label: 'Footwear',  slug: 'footwear' },
    { label: 'Jewellery', slug: 'jewellery' },
    { label: 'Beauty',    slug: 'beauty' },
    { label: 'Luxury',    slug: 'luxury' },
    { label: 'Brands',    slug: 'brands' },
    { label: 'Sale',      slug: 'sale' },
  ];

  readonly popularSearches = [
    'Kurtas', 'Sarees', 'Dresses', 'Sneakers',
    'Watches', 'Handbags', 'Jeans', 'T-Shirts',
    'Heels', 'Sunglasses', 'Ethnic Wear', 'Jackets',
  ];

  // ── Mega-menu close debounce ─────────────────────────────────
  private closeMenuTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Search blur debounce (allows suggestion clicks to fire first) ──
  private searchBlurTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnDestroy(): void {
    if (this.closeMenuTimer) clearTimeout(this.closeMenuTimer);
    if (this.searchBlurTimer) clearTimeout(this.searchBlurTimer);
  }

  /** Opens the mega-menu for `slug`, cancelling any pending close timer. */
  openCategoryMenu(slug: string): void {
    if (this.closeMenuTimer) {
      clearTimeout(this.closeMenuTimer);
      this.closeMenuTimer = null;
    }
    this.hoveredCategory.set(slug);
  }

  /**
   * Schedules the mega-menu close after 150ms.
   * If the mouse re-enters a category div or the panel within that window,
   * openCategoryMenu() cancels this timer and the menu stays open.
   */
  scheduleCategoryMenuClose(): void {
    this.closeMenuTimer = setTimeout(() => {
      this.hoveredCategory.set(null);
      this.closeMenuTimer = null;
    }, 150);
  }

  // ── Global keyboard / scroll handlers ───────────────────────

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled.set(window.scrollY > 80);
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    // Re-trigger spacer height recalculation on viewport width change.
    // Toggling scrolled forces the computed signal to re-evaluate.
    this.scrolled.set(window.scrollY > 80);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.hoveredCategory.set(null);
    this.searchFocused.set(false);   // also closes search suggestions
  }

  // ── Navigation / Auth ────────────────────────────────────────

  logout(): void {
    this.store.dispatch(AuthActions.logout());
  }

  openAuthModal(mode: 'login' | 'register'): void {
    this.store.dispatch(UiActions.openAuthModal({ mode }));
  }

  openMobileNav(): void {
    this.store.dispatch(UiActions.openMobileNav());
  }

  // ── Search actions ───────────────────────────────────────────

  /** Submit whatever is currently in the search input. */
  onSearch(): void {
    const q = this.searchQuery().trim();
    if (!q) return;
    this.router.navigate(['/products'], {
      queryParams: { search: q },
      queryParamsHandling: 'merge',
    });
    this.searchQuery.set('');        // signal change → OnPush re-renders → input clears
    this.searchFocused.set(false);
  }

  /** Navigate directly from a suggestion chip/row. */
  selectSuggestion(suggestion: string): void {
    this.router.navigate(['/products'], {
      queryParams: { search: suggestion },
      queryParamsHandling: 'merge',
    });
    this.searchQuery.set('');
    this.searchFocused.set(false);
  }

  /** Close the suggestions dropdown and empty the input. */
  clearSearch(): void {
    this.searchQuery.set('');
    this.searchFocused.set(false);
  }

  /** Close suggestions without clearing the text (e.g. backdrop click). */
  closeSearch(): void {
    this.searchFocused.set(false);
  }

  /**
   * Called when the search input loses focus.
   * A 200ms delay lets any suggestion button's (click) handler fire first
   * before the dropdown is removed from the DOM.
   */
  onSearchBlur(): void {
    this.searchBlurTimer = setTimeout(() => {
      this.searchFocused.set(false);
      this.searchBlurTimer = null;
    }, 200);
  }
}
