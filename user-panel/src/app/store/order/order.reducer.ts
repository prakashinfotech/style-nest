import { createReducer, on } from '@ngrx/store';
import { OosItem, OrderActions } from './order.actions';

export interface OrderState {
  isLoading:       boolean;
  error:           string | null;
  lastOrderId:     string | null;
  lastOrderNumber: string | null;
  outOfStockItems: OosItem[];
}

export const initialOrderState: OrderState = {
  isLoading:       false,
  error:           null,
  lastOrderId:     null,
  lastOrderNumber: null,
  outOfStockItems: [],
};

export const orderReducer = createReducer(
  initialOrderState,

  on(OrderActions.buyNow, (state) => ({ ...state, isLoading: true, error: null })),

  on(OrderActions.buyNowSuccess, (state, { orderId, orderNumber }) => ({
    ...state, isLoading: false, lastOrderId: orderId, lastOrderNumber: orderNumber, outOfStockItems: [],
  })),

  on(OrderActions.buyNowFailure, (state, { error, outOfStockItems }) => ({
    ...state, isLoading: false, error, outOfStockItems: outOfStockItems ?? [],
  })),

  on(OrderActions.placeOrder, (state) => ({
    ...state, isLoading: true, error: null, outOfStockItems: [],
  })),

  on(OrderActions.placeOrderSuccess, (state, { orderId, orderNumber }) => ({
    ...state, isLoading: false, lastOrderId: orderId, lastOrderNumber: orderNumber, outOfStockItems: [],
  })),

  on(OrderActions.placeOrderFailure, (state, { error, outOfStockItems }) => ({
    ...state, isLoading: false, error, outOfStockItems: outOfStockItems ?? [],
  })),

  on(OrderActions.clearOosItems, (state) => ({ ...state, outOfStockItems: [] })),
);
