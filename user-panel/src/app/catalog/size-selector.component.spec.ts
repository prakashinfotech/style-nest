import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { SizeSelectorComponent } from './size-selector.component';
import { ProductVariant } from '../core/models/product.model';

function makeVariant(size: string, stock: number, colour = 'Red'): ProductVariant {
  return { id: `${size}-${colour}`, size, colour, stockQuantity: stock, priceOverride: null };
}

describe('SizeSelectorComponent', () => {
  let fixture: ComponentFixture<SizeSelectorComponent>;
  let component: SizeSelectorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SizeSelectorComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(SizeSelectorComponent);
    component = fixture.componentInstance;
  });

  // ── uniqueSizes ─────────────────────────────────────────────────────────

  it('returns unique sizes excluding ONE SIZE', () => {
    component.variants = [
      makeVariant('S', 5),
      makeVariant('M', 3),
      makeVariant('M', 2, 'Blue'), // duplicate M
      makeVariant('ONE SIZE', 10),
    ];
    expect(component.uniqueSizes).toEqual(['S', 'M']);
  });

  it('returns empty array when all variants are ONE SIZE', () => {
    component.variants = [makeVariant('ONE SIZE', 5)];
    expect(component.uniqueSizes).toHaveLength(0);
  });

  it('preserves insertion order', () => {
    component.variants = [
      makeVariant('XL', 5),
      makeVariant('S', 5),
      makeVariant('M', 5),
    ];
    expect(component.uniqueSizes).toEqual(['XL', 'S', 'M']);
  });

  // ── isOutOfStock ────────────────────────────────────────────────────────

  it('returns true when all variants for a size have stockQuantity 0', () => {
    component.variants = [
      makeVariant('M', 0, 'Red'),
      makeVariant('M', 0, 'Blue'),
    ];
    expect(component.isOutOfStock('M')).toBe(true);
  });

  it('returns false when at least one variant for a size has stock > 0', () => {
    component.variants = [
      makeVariant('M', 0, 'Red'),
      makeVariant('M', 3, 'Blue'),
    ];
    expect(component.isOutOfStock('M')).toBe(false);
  });

  it('returns false for a size with no variants', () => {
    component.variants = [makeVariant('L', 5)];
    expect(component.isOutOfStock('M')).toBe(false);
  });

  // ── getLowStock ─────────────────────────────────────────────────────────

  it('returns total stock when 0 < total < 5', () => {
    component.variants = [
      makeVariant('S', 2, 'Red'),
      makeVariant('S', 1, 'Blue'),
    ];
    expect(component.getLowStock('S')).toBe(3);
  });

  it('returns null when total stock >= 5', () => {
    component.variants = [makeVariant('M', 10)];
    expect(component.getLowStock('M')).toBeNull();
  });

  it('returns null when total stock is 0 (OOS)', () => {
    component.variants = [makeVariant('L', 0)];
    expect(component.getLowStock('L')).toBeNull();
  });

  it('returns null for unknown size', () => {
    component.variants = [makeVariant('M', 3)];
    expect(component.getLowStock('XL')).toBeNull();
  });

  // ── sizeChange output ───────────────────────────────────────────────────

  it('emits sizeChange when a non-OOS size is clicked', () => {
    component.variants = [makeVariant('M', 5)];
    fixture.detectChanges();

    const emitted: string[] = [];
    component.sizeChange.subscribe((s) => emitted.push(s));

    // Simulate click on M chip
    component.sizeChange.emit('M');
    expect(emitted).toEqual(['M']);
  });

  // ── sizeGuideClick output ───────────────────────────────────────────────

  it('emits sizeGuideClick when Size Guide is clicked', () => {
    component.variants = [makeVariant('M', 5)];
    fixture.detectChanges();

    let clicked = false;
    component.sizeGuideClick.subscribe(() => (clicked = true));
    component.sizeGuideClick.emit();
    expect(clicked).toBe(true);
  });
});
