import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartItem } from '../core/models/cart.model';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';

@Component({
  selector: 'app-cart-item',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe],
  template: `
    <article class="flex gap-4 py-4 border-b border-gray-100 last:border-0" [attr.aria-label]="item.name">
      <!-- Image -->
      <a [routerLink]="['/products', item.productId]" class="flex-shrink-0">
        @if (item.imageUrl) {
          <img [src]="item.imageUrl" [alt]="item.name"
               class="w-20 h-24 md:w-24 md:h-28 object-cover rounded-lg bg-gray-50" loading="lazy" />
        } @else {
          <div class="w-20 h-24 md:w-24 md:h-28 bg-gray-100 rounded-lg flex items-center justify-center text-3xl">🛍️</div>
        }
      </a>

      <!-- Info -->
      <div class="flex-1 min-w-0">
        <a [routerLink]="['/products', item.productId]"
           class="text-sm md:text-base font-medium text-dark hover:text-blue transition line-clamp-2">
          {{ item.name }}
        </a>

        @if (item.size || item.colour) {
          <p class="text-xs text-muted mt-1">
            @if (item.size)   { Size: {{ item.size }} }
            @if (item.colour) { &nbsp;· Colour: {{ item.colour }} }
          </p>
        }

        <!-- Price -->
        <div class="flex items-center gap-2 mt-2">
          <span class="font-bold text-dark text-sm">{{ (item.salePrice ?? item.price) | currencyInr }}</span>
          @if (item.salePrice != null && item.salePrice < item.price) {
            <span class="text-xs text-muted line-through">{{ item.price | currencyInr }}</span>
          }
        </div>

        <!-- Quantity + Remove -->
        <div class="flex items-center justify-between mt-3">
          <div class="flex items-center border border-gray-200 rounded overflow-hidden">
            <button
              class="w-8 h-8 flex items-center justify-center hover:bg-gray-50 text-lg disabled:opacity-40"
              [disabled]="item.quantity <= 1"
              aria-label="Decrease quantity"
              (click)="quantityChange.emit(item.quantity - 1)"
            >−</button>
            <span class="w-10 text-center text-sm font-semibold">{{ item.quantity }}</span>
            <button
              class="w-8 h-8 flex items-center justify-center hover:bg-gray-50 text-lg disabled:opacity-40"
              [disabled]="item.quantity >= 10"
              aria-label="Increase quantity"
              (click)="quantityChange.emit(item.quantity + 1)"
            >+</button>
          </div>

          <div class="flex items-center gap-3">
            <button
              class="text-xs text-muted hover:text-navy hover:underline font-medium"
              aria-label="Save for later"
              (click)="saveForLater.emit()"
            >Save for later</button>
            <span class="text-border">|</span>
            <button
              class="text-xs text-red hover:underline font-medium"
              aria-label="Remove item"
              (click)="remove.emit()"
            >Remove</button>
          </div>
        </div>
      </div>

      <!-- Subtotal -->
      <div class="flex-shrink-0 text-right">
        <p class="text-sm font-bold text-dark">
          {{ (item.salePrice ?? item.price) * item.quantity | currencyInr }}
        </p>
      </div>
    </article>
  `,
})
export class CartItemComponent {
  @Input({ required: true }) item!: CartItem;
  @Output() quantityChange = new EventEmitter<number>();
  @Output() remove         = new EventEmitter<void>();
  @Output() saveForLater   = new EventEmitter<void>();
}
