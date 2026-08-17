import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OverlayModule } from '@angular/cdk/overlay';
import { ProductImagesComponent } from './product-images.component';

const IMAGES = [
  'https://example.com/img1.jpg',
  'https://example.com/img2.jpg',
  'https://example.com/img3.jpg',
];

describe('ProductImagesComponent', () => {
  let fixture: ComponentFixture<ProductImagesComponent>;
  let component: ProductImagesComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductImagesComponent, OverlayModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductImagesComponent);
    component = fixture.componentInstance;
    component.images = [...IMAGES];
    component.productName = 'Test Product';
    fixture.detectChanges();
  });

  // ── Initial state ───────────────────────────────────────────────────────

  it('starts at activeIndex 0', () => {
    expect(component.activeIndex()).toBe(0);
  });

  it('starts with imageLoaded false', () => {
    expect(component.imageLoaded()).toBe(false);
  });

  // ── Thumbnail click ─────────────────────────────────────────────────────

  it('updates activeIndex when thumbnail is clicked', () => {
    component.activeIndex.set(2);
    expect(component.activeIndex()).toBe(2);
  });

  it('sets activeIndex to 1 on second thumbnail click', () => {
    component.activeIndex.set(1);
    fixture.detectChanges();
    expect(component.activeIndex()).toBe(1);
  });

  // ── next() / prev() ─────────────────────────────────────────────────────

  it('next() advances activeIndex', () => {
    component.next();
    expect(component.activeIndex()).toBe(1);
  });

  it('next() wraps around from last to first', () => {
    component.activeIndex.set(IMAGES.length - 1);
    component.next();
    expect(component.activeIndex()).toBe(0);
  });

  it('prev() goes back one image', () => {
    component.activeIndex.set(2);
    component.prev();
    expect(component.activeIndex()).toBe(1);
  });

  it('prev() wraps around from first to last', () => {
    component.activeIndex.set(0);
    component.prev();
    expect(component.activeIndex()).toBe(IMAGES.length - 1);
  });

  it('next() resets imageLoaded to false', () => {
    component.imageLoaded.set(true);
    component.next();
    expect(component.imageLoaded()).toBe(false);
  });

  it('prev() resets imageLoaded to false', () => {
    component.imageLoaded.set(true);
    component.prev();
    expect(component.imageLoaded()).toBe(false);
  });

  // ── Touch swipe ─────────────────────────────────────────────────────────

  it('swipe left (negative delta) calls next()', () => {
    const startEvent = { changedTouches: [{ clientX: 200 }] } as unknown as TouchEvent;
    const endEvent   = { changedTouches: [{ clientX: 100 }] } as unknown as TouchEvent;

    component.onTouchStart(startEvent);
    component.onTouchEnd(endEvent);

    expect(component.activeIndex()).toBe(1);
  });

  it('swipe right (positive delta) calls prev()', () => {
    component.activeIndex.set(2);
    const startEvent = { changedTouches: [{ clientX: 100 }] } as unknown as TouchEvent;
    const endEvent   = { changedTouches: [{ clientX: 200 }] } as unknown as TouchEvent;

    component.onTouchStart(startEvent);
    component.onTouchEnd(endEvent);

    expect(component.activeIndex()).toBe(1);
  });

  it('ignores swipe with delta < 40px', () => {
    const startEvent = { changedTouches: [{ clientX: 100 }] } as unknown as TouchEvent;
    const endEvent   = { changedTouches: [{ clientX: 130 }] } as unknown as TouchEvent;

    component.onTouchStart(startEvent);
    component.onTouchEnd(endEvent);

    expect(component.activeIndex()).toBe(0); // unchanged
  });

  // ── Empty images ────────────────────────────────────────────────────────

  it('handles empty images array without error', () => {
    component.images = [];
    fixture.detectChanges();
    expect(component.activeIndex()).toBe(0);
  });
});
