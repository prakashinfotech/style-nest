import { createFeatureSelector, createSelector } from '@ngrx/store';
import { OrderState } from './order.reducer';

export const selectOrderState    = createFeatureSelector<OrderState>('order');
export const selectOrderLoading  = createSelector(selectOrderState, (s) => s.isLoading);
export const selectOrderError    = createSelector(selectOrderState, (s) => s.error);
export const selectLastOrderId   = createSelector(selectOrderState, (s) => s.lastOrderId);
export const selectLastOrderNumber = createSelector(selectOrderState, (s) => s.lastOrderNumber);
export const selectOutOfStockItems = createSelector(selectOrderState, (s) => s.outOfStockItems);
