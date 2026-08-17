import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AsyncPipe, CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { CartActions } from '../../store/cart/cart.actions';
import { selectCart, selectCartLoading } from '../../store/cart/cart.selectors';
import { OrderActions } from '../../store/order/order.actions';
import { selectOrderLoading, selectOutOfStockItems } from '../../store/order/order.selectors';
import { AddressStepComponent, DeliveryAddress } from '../../checkout/address-step.component';
import { PaymentStepComponent, PaymentMethod } from '../../checkout/payment-step.component';
import { OrderSummaryComponent } from '../../checkout/order-summary.component';
import { OrderConfirmationComponent } from '../../checkout/order-confirmation.component';
import { SkeletonLoaderComponent } from '../../shared/components/skeleton-loader.component';

type CheckoutStep = 'address' | 'payment' | 'confirmation';

@Component({
  selector: 'app-checkout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, AsyncPipe,
    AddressStepComponent, PaymentStepComponent,
    OrderSummaryComponent, OrderConfirmationComponent,
    SkeletonLoaderComponent,
  ],
  template: `
    <div class="max-w-layout mx-auto px-4 py-6 min-h-screen">
      <h1 class="text-xl md:text-2xl font-bold text-dark mb-6">Checkout</h1>

      <!-- Steps breadcrumb -->
      @if (currentStep() !== 'confirmation') {
        <div class="flex items-center gap-2 text-sm mb-6">
          @for (step of steps; track step.key; let i = $index) {
            <div class="flex items-center gap-2">
              <div
                class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold"
                [class.bg-navy]="isStepActive(step.key) || isStepDone(step.key)"
                [class.text-white]="isStepActive(step.key) || isStepDone(step.key)"
                [class.bg-gray-200]="!isStepActive(step.key) && !isStepDone(step.key)"
                [class.text-muted]="!isStepActive(step.key) && !isStepDone(step.key)"
              >{{ i + 1 }}</div>
              <span [class.font-semibold]="isStepActive(step.key)" [class.text-navy]="isStepActive(step.key)"
                    [class.text-muted]="!isStepActive(step.key)">{{ step.label }}</span>
            </div>
            @if (i < steps.length - 1) {
              <div class="flex-1 h-px bg-gray-200 max-w-12"></div>
            }
          }
        </div>
      }

      <div class="flex flex-col lg:flex-row gap-6">
        <!-- Main form area -->
        <div class="flex-1">
          @switch (currentStep()) {
            @case ('address') {
              <div class="bg-card rounded-lg border border-gray-100 p-5">
                <app-address-step (addressSubmit)="onAddressSubmit($event)" />
              </div>
            }
            @case ('payment') {
              <div class="bg-card rounded-lg border border-gray-100 p-5">

                <!-- OOS banner — shown when inventory re-validation fails -->
                @if (outOfStockItems$ | async; as oosItems) {
                  @if (oosItems.length > 0) {
                    <div class="bg-red/10 border border-red rounded-lg p-4 mb-5">
                      <p class="text-sm font-semibold text-red mb-2">
                        Some items are unavailable in the requested quantity:
                      </p>
                      <ul class="space-y-1 mb-3">
                        @for (item of oosItems; track item.variantId) {
                          <li class="text-xs text-dark">
                            <span class="font-medium">{{ item.productName }}</span>
                            @if (item.variantDetails) {
                              <span class="text-muted"> ({{ item.variantDetails }})</span>
                            }
                            — requested {{ item.requestedQuantity }},
                            @if (item.availableQuantity === 0) {
                              <span class="text-red font-medium">out of stock</span>
                            } @else {
                              only {{ item.availableQuantity }} left
                            }
                          </li>
                        }
                      </ul>
                      <p class="text-xs text-muted">
                        Please go back to your cart and remove or reduce the affected items.
                      </p>
                    </div>
                  }
                }

                @if (isOrderLoading$ | async) {
                  <div class="flex items-center justify-center py-8">
                    <div class="w-8 h-8 border-4 border-navy border-t-transparent rounded-full animate-spin"></div>
                    <span class="ml-3 text-sm text-muted">Placing your order…</span>
                  </div>
                } @else {
                  <app-payment-step (paymentSubmit)="onPaymentSubmit($event)" />
                }
              </div>
            }
            @case ('confirmation') {
              <app-order-confirmation [orderId]="confirmedOrderId()" />
            }
          }
        </div>

        <!-- Order summary sidebar (hide on confirmation) -->
        @if (currentStep() !== 'confirmation') {
          <div class="lg:w-80">
            @if (isLoading$ | async) {
              <app-skeleton-loader height="300px" cssClass="rounded-lg" />
            } @else if (cart$ | async; as cart) {
              <app-order-summary [cart]="cart" />
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class CheckoutComponent implements OnInit {
  private readonly store = inject(Store);

  readonly cart$             = this.store.select(selectCart);
  readonly isLoading$        = this.store.select(selectCartLoading);
  readonly isOrderLoading$   = this.store.select(selectOrderLoading);
  readonly outOfStockItems$  = this.store.select(selectOutOfStockItems);

  readonly currentStep      = signal<CheckoutStep>('address');
  readonly confirmedOrderId = signal<string | null>(null);

  private deliveryAddress: DeliveryAddress | null = null;

  readonly steps: Array<{ key: CheckoutStep; label: string }> = [
    { key: 'address', label: 'Address' },
    { key: 'payment', label: 'Payment' },
  ];

  ngOnInit(): void {
    this.store.dispatch(CartActions.loadCart());
  }

  isStepActive(step: CheckoutStep): boolean {
    return this.currentStep() === step;
  }

  isStepDone(step: CheckoutStep): boolean {
    const order: CheckoutStep[] = ['address', 'payment', 'confirmation'];
    return order.indexOf(step) < order.indexOf(this.currentStep());
  }

  onAddressSubmit(address: DeliveryAddress): void {
    this.deliveryAddress = address;
    this.currentStep.set('payment');
  }

  onPaymentSubmit(method: PaymentMethod): void {
    if (!this.deliveryAddress) return;
    this.store.dispatch(OrderActions.placeOrder({
      addressLine1:  this.deliveryAddress.addressLine1,
      addressLine2:  this.deliveryAddress.addressLine2,
      city:          this.deliveryAddress.city,
      state:         this.deliveryAddress.state,
      pincode:       this.deliveryAddress.pincode,
      paymentMethod: method,
    }));
  }
}
