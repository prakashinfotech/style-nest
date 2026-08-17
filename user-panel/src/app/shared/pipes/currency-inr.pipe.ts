import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'currencyInr', standalone: true, pure: true })
export class CurrencyInrPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) return '—';
    return new Intl.NumberFormat('en-IN', {
      style:    'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(value);
  }
}
