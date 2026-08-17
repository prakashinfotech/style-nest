# DESIGN.md — StyleNest Fashion Website Replica
> Production-grade design specification for a modern, professional e-commerce SPA.
> All components implemented in Angular 21 · Tailwind CSS 3 · TypeScript strict mode.

---

## 1. Brand Overview

**Platform**: StyleNest Fashion (rebranded late 2024)
**Parent**: Tata Digital Private Limited (Tata Group)
**Category**: Premium Indian fashion & lifestyle e-commerce
**Positioning**: Curated, trust-led, phygital (physical + digital) shopping experience
**Audience**: Middle and upper-middle income, tech-savvy, 22–45 year olds in Tier-I & Tier-II cities
**Tagline feel**: "Magic & Joy in Shopping" — premium but accessible, authentic, curated

---

## 2. Visual Identity

### 2.1 Color Palette

```css
:root {
  /* Primary Brand */
  --sn-red:        #E31837;   /* Primary CTA, logo accent, sale badges */
  --sn-dark:       #1A1A1A;   /* Primary text, headers */
  --sn-white:      #FFFFFF;   /* Backgrounds, cards */

  /* Secondary */
  --sn-navy:       #1C2B4A;   /* Nav bar background, footer */
  --sn-gold:       #C9A84C;   /* Luxury accents, NeuCoins, seller */
  --sn-light-gray: #F5F5F5;   /* Page background, section fills */
  --sn-mid-gray:   #9E9E9E;   /* Secondary text, placeholders, borders */
  --sn-border:     #E0E0E0;   /* Dividers, card outlines */

  /* Functional */
  --sn-success:    #2E7D32;   /* In stock, order confirmed */
  --sn-warning:    #F57C00;   /* Low stock, expiring offer */
  --sn-error:      #C62828;   /* Out of stock, errors */
  --sn-discount:   #E31837;   /* Discount % badge */
  --sn-blue:       #0071C2;   /* CTA secondary, links */

  /* Gradients */
  --sn-hero-gradient: linear-gradient(135deg, #1C2B4A 0%, #2C3E70 100%);
  --sn-sale-gradient: linear-gradient(90deg, #E31837 0%, #FF6B35 100%);
  --sn-luxury-gradient: linear-gradient(135deg, #C9A84C 0%, #8B6914 100%);
  --sn-card-gradient: linear-gradient(to top, rgba(0,0,0,0.6) 0%, transparent 60%);
}
```

**Usage rules:**
- `--sn-red` is the single primary action colour — use sparingly for maximum impact
- Never use pure black (#000000) — use `--sn-dark` (#1A1A1A) instead
- Button hover states darken by 8–10% (e.g. `#C91030` for red hover)
- Disabled states use `--sn-mid-gray` on `--sn-light-gray` background

### 2.2 Typography

```css
/* Import from Google Fonts — self-host in production */
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700&family=DM+Sans:wght@300;400;500;600&display=swap');

:root {
  /* Display / Headings — editorial feel */
  --font-display: 'Playfair Display', Georgia, serif;

  /* Body / UI — clean and modern */
  --font-body: 'DM Sans', -apple-system, sans-serif;

  /* Type Scale */
  --text-xs:   11px;   /* labels, badges, meta */
  --text-sm:   13px;   /* secondary info, MRP */
  --text-base: 15px;   /* body copy, prices */
  --text-md:   17px;   /* feature text */
  --text-lg:   20px;   /* sub-headings */
  --text-xl:   24px;   /* section titles */
  --text-2xl:  32px;   /* page headings */
  --text-3xl:  42px;   /* hero headline desktop */
  --text-4xl:  56px;   /* super-hero headline */

  /* Weight */
  --weight-light:   300;
  --weight-regular: 400;
  --weight-medium:  500;
  --weight-semi:    600;
  --weight-bold:    700;

  /* Line heights */
  --leading-tight:  1.2;
  --leading-snug:   1.35;
  --leading-normal: 1.5;
  --leading-loose:  1.8;

  /* Letter spacing */
  --tracking-normal:  0;
  --tracking-wide:    0.05em;  /* Navigation labels, badges */
  --tracking-wider:   0.08em;  /* Category tags */
  --tracking-widest:  0.12em;  /* All-caps labels, eyebrows */
}
```

**Typography hierarchy:**
| Role | Font | Size | Weight | Usage |
|------|------|------|--------|-------|
| Hero headline | Playfair Display | 40–56px | Bold | Carousel, editorial banners |
| Section title | Playfair Display | 24–32px | SemiBold | Homepage sections |
| Product title | DM Sans | 14px | Medium | Product cards |
| PDP title | Playfair Display | 26px | Regular | Product detail |
| Body | DM Sans | 15px | Regular | Descriptions, paragraphs |
| Meta / Label | DM Sans | 11–13px | Medium | Badges, brand names, dates |
| Price | DM Sans | 15px | SemiBold | All price displays |

---

## 3. Layout System

### 3.1 Grid & Spacing

```css
:root {
  --container-max:    1280px;
  --container-pad:    24px;     /* Desktop side padding */
  --container-pad-sm: 16px;     /* Mobile side padding */

  /* Spacing scale (8px base) */
  --space-1:  4px;
  --space-2:  8px;
  --space-3:  12px;
  --space-4:  16px;
  --space-5:  20px;
  --space-6:  24px;
  --space-8:  32px;
  --space-10: 40px;
  --space-12: 48px;
  --space-16: 64px;
  --space-20: 80px;

  /* Section rhythm */
  --section-gap:    48px;   /* Desktop gap between page sections */
  --section-gap-sm: 32px;   /* Mobile section gap */
  --section-gap-xs: 20px;   /* Tight sections (mobile) */

  /* Card grid columns */
  --grid-cols-4: repeat(4, 1fr);   /* Product listing desktop */
  --grid-cols-3: repeat(3, 1fr);   /* Featured / collection desktop */
  --grid-cols-2: repeat(2, 1fr);   /* Mobile product grid */
  --grid-cols-1: 1fr;              /* Mobile single column */
}
```

### 3.2 Border Radius

```css
:root {
  --radius-sm:   4px;    /* Badges, tags, small chips */
  --radius-md:   8px;    /* Cards, buttons, inputs */
  --radius-lg:   12px;   /* Modal corners, image cards */
  --radius-xl:   20px;   /* Pill buttons, featured cards */
  --radius-2xl:  24px;   /* Large promo cards */
  --radius-full: 9999px; /* Circular elements, round badges */
}
```

### 3.3 Shadows & Elevation

```css
:root {
  /* Elevation ladder — use consistently */
  --shadow-xs:         0 1px 2px rgba(0,0,0,0.06);    /* subtle, inline */
  --shadow-sm:         0 2px 8px rgba(0,0,0,0.08);    /* default card */
  --shadow-md:         0 4px 16px rgba(0,0,0,0.10);   /* raised card */
  --shadow-lg:         0 8px 32px rgba(0,0,0,0.12);   /* dropdown, popover */
  --shadow-xl:         0 16px 48px rgba(0,0,0,0.16);  /* modal */
  --shadow-card-hover: 0 8px 24px rgba(0,0,0,0.14);   /* product card hover */
  --shadow-sticky:     0 2px 12px rgba(0,0,0,0.10);   /* sticky header on scroll */
}
```

---

## 4. Component Specifications

### 4.1 Announcement Bar

```
Height:           36px (h-9)
Background:       var(--sn-red)
Text:             13px DM Sans Medium, white (#FFF), centered
Content (rotate): Cycle 3–4 promotional messages every 4s
Dismissible:      ✓ — × icon right-aligned, stores dismiss in sessionStorage
ARIA:             role="alert" aria-live="polite"
Animation:        Slide in from top on mount; slide out on dismiss
```

### 4.2 Primary Navigation (Sticky + Scroll-Aware)

```
Height:           64px desktop (h-16), 56px mobile (h-14)
Compressed:       52px after 100px scroll (transition 200ms)
Background:       #FFFFFF
Border-bottom:    1px solid var(--sn-border) — hidden when announcement bar visible
Box-shadow:       var(--shadow-sticky) on scroll (opacity transition)
Position:         sticky top-0 z-50

Layout (LTR):
  [Hamburger — mobile] [Logo] ··· [Search — flex-1 max-w-xl] ··· [Wishlist | Bag | Account]

Logo:
  - "TATA StyleNest" — navy "TATA" + red "StyleNest", Playfair Display 20–24px bold
  - Aria-label: "StyleNest Fashion — go to homepage"

Search Bar:
  - Width: full flex-1 (max 480px desktop), full-width mobile (below top bar)
  - Background: var(--sn-light-gray) — border: var(--sn-border)
  - focus-within: border-color switches to var(--sn-red)
  - Border-radius: var(--radius-full) — pill shape
  - Search icon: left-inset 16px
  - Submit: red "Search" button flush right inside pill
  - Placeholder: "Search for products, brands and more"

Category Nav (second row, desktop only):
  - Border-top: 1px solid var(--sn-border)
  - Labels: Women | Men | Kids | Beauty | Home | Brands | Sale | Luxury
  - Font: 14px DM Sans Medium, var(--sn-dark)
  - Hover: text → var(--sn-red), animated underline via ::after scale-x
  - Active route: underline in var(--sn-red) (permanent)

Icon cluster (right):
  - Icon size: 24px
  - Label: 11px DM Sans, tracking-widest, UPPERCASE
  - Cart badge: 16px circle, var(--sn-red) bg, white count, -top-1.5 -right-1.5
  - Min tap target: 44×44px
```

### 4.3 Mega Menu Dropdown

```
Trigger:    Hover on desktop category tab (300ms delay before open)
Width:      100vw full-bleed
Background: white
Shadow:     var(--shadow-lg)
Padding:    32px var(--container-pad)
Animation:  translateY(-8px → 0) + opacity(0 → 1), 200ms ease-out

Three-column layout:
  Left (40%):  Sub-category links grouped under bold headers
  Center (35%): "Top Brands" — 3×2 brand logo tiles, 80×80px, bordered circles
  Right (25%): Editorial promo image (400×280px) + "Shop Now" CTA overlay

Links: 14px DM Sans, hover → var(--sn-red)
Headers: 13px DM Sans SemiBold, tracking-wide, UPPERCASE, mid-gray
```

### 4.4 Hero Banner / Carousel

```
Height:         480px desktop (h-[480px]) | 260px mobile (h-[260px])
Type:           Full-width crossfade carousel with slide indicator progress bar
Auto-advance:   5 seconds (paused on hover, keyboard focus)
Transition:     Crossfade opacity (400ms ease-in-out)
Controls:
  - Dot indicators: bottom-center, active dot expands width (w-2 → w-6) + opacity
  - Progress bar:   thin 3px bar running across bottom of active slide (5s fill)
  - Arrow chevrons: sides, 44×44px min tap, semi-transparent white circles
  - Keyboard:       ArrowLeft / ArrowRight when focused

Overlay: left-to-right gradient (black/75 → transparent)
Text block (bottom-left):
  - Eyebrow:  12px DM Sans Medium, ALL CAPS, tracking-widest, white/80
  - Headline: 40px desktop / 22px mobile, Playfair Display Bold, white
  - Sub-text: 16px / hidden on mobile, DM Sans Light, white/70
  - CTA:      var(--sn-red) bg, white text, border-radius var(--radius-md), 48px height

Aspect ratio: 16:5 desktop | 4:3 mobile
ARIA: aria-roledescription="carousel", each slide: role="group"
```

### 4.5 Category Shortcut Strip

```
Layout:     Horizontal scroll strip (8–12 circular items with labels)
Item:       72×72px circle image / icon | 12px DM Sans label below, text-center
Container:  overflow-x auto, scrollbar hidden (scrollbar-width: none)
Gap:        16–20px between items
Hover:      Scale(1.08) + shadow-sm on circle, transition 300ms
Active:     Ring 2px var(--sn-red) around circle
Padding:    16px vertical, container horizontal
```

### 4.6 Section Header

```
Layout:   flex row — [Eyebrow + Title stack] [View All →]
Eyebrow:  11px DM Sans Medium, ALL CAPS, tracking-widest, var(--sn-red), mb-1
Title:    24px–32px Playfair Display SemiBold, var(--sn-dark)
Divider:  2px × 40px var(--sn-red) bar under title (left-aligned, mt-2)
View All: 13px DM Sans Medium, var(--sn-red), hover underline, "View All →"
Spacing:  mb-6 below section header before grid
```

### 4.7 Product Card

```
Width:        auto (grid-defined — fluid)
Aspect ratio: Image 3:4 (portrait, fashion standard)
Border-radius: var(--radius-md)
Background:   white
Overflow:     hidden
Transition:   box-shadow 300ms ease

Image container:
  - object-fit: cover, w-full h-full
  - overflow: hidden
  - Hover: scale(1.04), transition 300ms ease

Badges (top-left, pill):
  - Discount: var(--sn-red) bg, white text, 11px, "XX% off"
  - New:      var(--sn-navy) bg, white text, 11px, "NEW"
  - z-index: 10

Wishlist button (top-right):
  - 34×34px circle, white/80 bg (backdrop-blur optional)
  - Hover: white bg
  - Default state:    hollow heart, text-mid-gray
  - Wishlist state:   filled heart, text-red
  - Click animation: heart-pop (scale 1 → 1.35 → 1, 300ms)
  - opacity-0 → group-hover:opacity-100, transition 200ms
  - Always visible on touch devices

Quick View (bottom of image):
  - translateY(100%) → group-hover:translateY(0), 300ms ease
  - Dark overlay button: bg-dark/90, white text 12px Medium
  - min-height 36px

Info block (padding: pt-3 pb-2 px-1):
  Brand:      11px DM Sans UPPERCASE tracking-widest, mid-gray, truncate
  Name:       14px DM Sans Medium, dark, 2-line clamp, hover → red, mb-1.5
  Price row:  flex wrap, gap-2
    Sell price: 15px DM Sans SemiBold, dark
    MRP:        13px DM Sans, line-through, mid-gray
    Discount %: 13px DM Sans Medium, red
  Rating:     12px, star icon + count, optional

Mobile adjustments:
  - Brand: 10px | Name: 13px | Sell price: 14px
  - No hover states (use touch events instead)
  - Wishlist always visible (no opacity toggle)
```

### 4.8 Brand Logo Strip

```
Layout:         Horizontal scroll or 6-col static grid (lg)
Item:           160×80px bordered box — 1px var(--sn-border) border
Background:     white
Border-radius:  var(--radius-md)
Logo:           Grayscale by default → Full colour on hover
Transition:     filter 300ms ease + shadow-sm on hover
Gap:            16px
Padding:        12px inside each tile (logo centred)
```

### 4.9 Promotional Banners (2-up / 3-up)

```
Layout:         CSS Grid, 2 or 3 equal columns
Gap:            16px (md: 20px)
Height:         220px (2-up desktop) | 180px (3-up desktop) | 160px mobile
Image:          Full cover, border-radius var(--radius-lg)
Overlay:        Bottom gradient: transparent → rgba(0,0,0,0.55)
Text:           White headline 18px Playfair Display + CTA link 13px DM Sans
Hover:          Scale image 1.03 + shadow-md, transition 300ms
ARIA:           role="figure" + aria-label describing the promo
```

### 4.10 Add to Cart / Buy Now Buttons

```
Add to Cart (secondary):
  Background:    white
  Border:        2px solid var(--sn-red)
  Text:          var(--sn-red), 15px DM Sans SemiBold
  Hover:         bg → var(--sn-red), text → white
  Active press:  scale(0.97), 100ms

Buy Now (primary):
  Background:    var(--sn-red)
  Text:          white, 15px DM Sans SemiBold
  Hover:         bg → #C91030 (darken 8%)
  Active press:  scale(0.97), 100ms

Both:
  Height:        48px (h-12) for PDP, 40px (h-10) for card
  Border-radius: var(--radius-md)
  Letter-spacing: 0.03em
  Transition:    all 200ms ease
  Width:         100% on PDP, flex-1 side-by-side
  Disabled:      opacity-40, cursor-not-allowed
```

### 4.11 Size Selector Chips

```
Type:   Pill/square chips (horizontal flex wrap, gap-2)
Size:   36×36px auto-width for text sizes
States:
  Default:  border-border text-dark bg-white, hover → border-navy
  Selected: border-red bg-red text-white
  Disabled: relative, diagonal strikethrough via ::after, text-mid-gray, opacity-50, no hover
Transition: border-color + background 150ms ease
Spacing:    gap-2 flex-wrap
```

### 4.12 Filter Sidebar

```
Width:      256px desktop (sticky on scroll)
Background: white
Padding:    16px
Border:     1px solid var(--sn-border), border-radius var(--radius-md)

Groups:
  Header:       13px DM Sans SemiBold tracking-wide UPPERCASE, cursor pointer
  Chevron:      rotates 180° when expanded (transition 200ms)
  Content:      collapsible with height transition (max-height approach)

Filter types:
  Category:   Radio list (16px tap targets)
  Price:       Range slider (red thumb + track)
  Brand:       Checkbox list with brand name
  Discount:    Radio chips (10%, 20%, 30%, 50%+)
  Color:       24px color swatches with tooltip label

Applied chip: bg-red/10 text-red border border-red/30, × remove button
Clear all:    text link, hover underline, red

Mobile:
  - Sheet drawer from left (translateX(-100% → 0), overlay dim)
  - "Done" button at bottom
  - Close × top-right
```

### 4.13 Sort Dropdown

```
Trigger:    "Sort by" button, border, 14px DM Sans
Dropdown:   white bg, shadow-lg, min-w 200px, border-radius var(--radius-md)
Options:    Relevance | Newest First | Price: Low → High | Price: High → Low | Discount
Active:     var(--sn-red) colour + checkmark icon right
Animation:  translateY(-4px → 0) + opacity, 150ms ease-out
```

### 4.14 Toast / Snackbar

```
Position:     Bottom-center (or bottom-right on desktop), 24px from edge
Width:        320px desktop | calc(100vw - 32px) mobile
Padding:      14px 20px
Background:   #1A1A1A (dark)
Border-radius: var(--radius-md)
Text:         14px DM Sans, white
Duration:     3500ms → auto-dismiss
Animation:    translateY(100% → 0) + opacity(0→1), 280ms ease-out on enter
             opacity(1→0) + translateY(0→8px), 200ms on exit
Icons:
  Success: ✓ circle — var(--sn-success) color
  Error:   ✕ circle — var(--sn-error)
  Info:    ℹ circle — var(--sn-blue)
  Warning: ⚠ — var(--sn-warning)
Dismiss:    Manual × button OR auto-dismiss after duration
Stack:      Max 3 toasts visible (LIFO stack from bottom)
ARIA:       role="alert" aria-live="assertive" for errors, "polite" for info
```

### 4.15 Skeleton Loader

```
Type:         Shimmer gradient animation
Colors:       #F0F0F0 → #E0E0E0 → #F0F0F0 (90-degree gradient)
Animation:    shimmer keyframe, 1.5s infinite linear
Border-radius: Match the element being loaded
Card skeleton: Aspect-[3/4] image block + 3 text lines (brand, name, price)
List skeleton: Horizontal bar with avatar circle at start
Pulse variant: For icon-only placeholders (opacity 1→0.4→1, 2s infinite)
```

### 4.16 Empty State

```
Container: flex-col items-center text-center, max-w-xs mx-auto, py-16
Icon:      64px, text-mid-gray (outline style)
Title:     20px Playfair Display, var(--sn-dark), mt-4
Subtitle:  14px DM Sans, text-muted, mt-2, max-w-[260px]
CTA:       var(--sn-red) button, mt-6, "Continue Shopping" / context action
```

### 4.17 Breadcrumb

```
Items:      Home › Category › Sub-category › Product
Separator:  › (›) — mid-gray, mx-1.5
Active:     Last item — text-dark, font-medium, not linked
Links:      text-muted, hover → text-red, underline on hover
Font:       13px DM Sans
ARIA:       nav aria-label="Breadcrumb" + aria-current="page" on last
```

### 4.18 Footer

```
Background: var(--sn-navy)
Text:       white / #B0BEC5 (secondary)
Max-width:  var(--container-max)

Layout (4 columns desktop, 2 columns tablet, 1 column mobile):
  Col 1 — Brand & Social:
    - Logo (Playfair Display, white)
    - Tagline (14px DM Sans Light, #B0BEC5)
    - Social icons: Instagram, Facebook, Twitter, YouTube (24px, hover → white/80)
  Col 2 — Shopping:
    - Links: Track Order | Returns | Size Guide | StyleNest Luxury | Gift Cards
  Col 3 — Help & Policies:
    - Links: Help Centre | Privacy Policy | T&C | Accessibility | Sitemap
  Col 4 — Download App:
    - App Store + Play Store badges (SVG)
    - "Shop on the go"

Column headings:  13px DM Sans SemiBold UPPERCASE tracking-wide, var(--sn-gold)
Link items:       14px DM Sans, #B0BEC5, hover → white, transition 150ms
Padding:          pt-12 pb-6 (desktop), pt-8 pb-4 (mobile)
Gap:              gap-8 (desktop), gap-6 (tablet), gap-4 (mobile)

Bottom bar (border-top border-navy-light):
  Left:   "© 2026 StyleNest. All rights reserved."
  Right:  Payment icons — Visa, Mastercard, UPI, PayTM, NetBanking (32px height)
  Font:   12px DM Sans, #78909C
  Padding: py-4
```

### 4.19 Back-to-Top Button

```
Position:     fixed bottom-6 right-6, z-50
Visibility:   opacity-0 → opacity-100 after 400px scroll (transition 300ms)
Size:         48×48px circle
Background:   var(--sn-navy)
Icon:         ChevronUp, white, 20px
Hover:        bg → var(--sn-red)
ARIA:         aria-label="Back to top"
```

---

## 5. Page Templates

### 5.1 Homepage

```
Section order (top → bottom):
  1.  Announcement Bar
  2.  Primary Navigation (sticky)
  3.  Hero Carousel (§4.4)
  4.  Category Shortcut Strip (§4.5) — 8 categories, horizontal scroll
  5.  Section "New Arrivals" — SectionHeader + 4-col ProductCard grid (live API, 8 products)
  6.  2-up Promo Banner — "Women's Picks" | "Men's Essentials" (§4.9)
  7.  Section "Top Brands" — SectionHeader + BrandLogoStrip (§4.8)
  8.  Section "Trending Now" — SectionHeader + horizontal scroll row (8 cards)
  9.  3-up Promo Banner — seasonal / category themed (§4.9)
  10. Section "Sale Picks" — SectionHeader + 4-col grid with red discount badges
  11. FlashSale countdown widget (if active)
  12. Editorial / Lifestyle full-width banner
  13. Footer (§4.18)

Spacing between sections: var(--section-gap) = 48px desktop, 32px mobile
Container: max-w-layout mx-auto px-4 md:px-6 for all sections
```

### 5.2 Product Listing Page (PLP)

```
URL:          /products?category=X&brand=Y&search=Z
Layout:       [Filter sidebar 256px] | [Results area flex-1]

Top bar:
  - Breadcrumb (§4.17)
  - H1 title (category name or "Search: X")
  - Result count + Sort dropdown (right)
  - Filter toggle (mobile)

Filter sidebar (§4.12):
  - Sticky at top: top-24 (below header)
  - Desktop: always visible
  - Mobile: drawer sheet

Product grid:
  - 4-col lg | 3-col md | 2-col sm/mobile
  - gap-4 between cards
  - ProductCard (§4.7) × n

Pagination:
  - Primary: page buttons (prev / 1 2 3 … / next)
  - Fallback: "Load More" button, var(--sn-red) outline
  - Scroll to top on page change

Empty state: EmptyStateComponent with "No products found" + clear filters CTA
Loading:     SkeletonLoader cards × 8 (3:4 aspect blocks)
```

### 5.3 Product Detail Page (PDP)

```
URL:          /products/:id
Layout:       2-col (60% image | 40% info) desktop — stacked mobile

Breadcrumb:   Home › Category › Brand › Product name

Image area (left):
  - Primary image (max-h 600px, object-contain)
  - Thumbnail strip: vertical left side, 4–6 thumbs, 72×96px
  - Click thumbnail → fade swap primary image (200ms opacity)
  - Hover: zoom overlay cursor
  - Mobile: horizontal swipe carousel

Info panel (right):
  - Brand name: 12px UPPERCASE tracking-widest, linked to brand page
  - Product title: 24–28px Playfair Display Regular
  - Rating row: ★ stars (filled gold) + "124 Reviews" link
  - Price block:
      Selling price: 20px DM Sans SemiBold
      MRP:           15px strikethrough mid-gray
      Discount %:    14px red badge pill "30% off"
  - Offers:      collapsible "Available Offers" section (§8.2)
  - NeuCoins:    gold icon + "Earn X NeuCoins" (§8.3)
  - Color selector (§ — visual swatches 24×24px + tooltip)
  - Size selector (§4.11 chips)
  - Size Chart:  text link, opens modal
  - Quantity:    - / [n] / + stepper (min 1, max 10)
  - Action row:  [Add to Wishlist ♡] [Add to Cart] [Buy Now] (§4.10)
  - Pincode estimator: "Enter pincode" input + "Check" button
  - StyleNest Promise badges (§8.1): 4 badges horizontal row

Below fold:
  - Product description (collapsible, default expanded)
  - Size & Fit (collapsible)
  - Reviews section (star histogram + review cards)
  - "You May Also Like" — horizontal scroll product row
  - Recently Viewed

Mobile:
  - Sticky ATC row (bottom 0, full width) — Add to Cart | Buy Now
  - Image: full-width swipe carousel
```

### 5.4 Cart Page

```
Layout:       2-col (items 60% | summary 40%) desktop — stacked mobile

Items list:
  - CartItemComponent × n
  - Remove × and quantity stepper per item
  - "Move to Wishlist" link
  - Out-of-stock alert per item if applicable

Order summary:
  - MRP total, Discount, Delivery, Coupon savings
  - Subtotal (bold)
  - Coupon input (§ — CouponInputComponent)
  - StyleNest Promise badges (§8.1)
  - "Proceed to Checkout" button — full width, var(--sn-red)

Empty state: EmptyStateComponent — cart icon + "Your bag is empty" + CTA
```

### 5.5 Checkout Flow

```
Steps:        Address → Payment → Confirmation
Stepper:      Progress indicator at top (3 steps, current highlighted red)

Address step:
  - Saved addresses radio list
  - "Add new address" form (collapsible)
  - Pincode auto-fill via API

Payment step:
  - Method: Credit/Debit Card | UPI | Net Banking | Wallet | COD
  - Secure padlock icon + "256-bit SSL" note

Confirmation:
  - Green check animation (CSS only, no JS library)
  - Order number, estimated delivery date
  - "Continue Shopping" CTA
```

---

## 6. Motion & Micro-Interactions Catalog

```
Design Principles:
  - Purposeful — motion communicates state, not decoration
  - Fast — 150–300ms for UI, 400ms for page elements, never > 500ms
  - Ease-out for enters | ease-in for exits | ease-in-out for transforms

Animation Dictionary:
  Name                Duration  Easing      Trigger
  ─────────────────── ──────── ──────────── ──────────────────────
  hero-crossfade      400ms    ease-in-out  auto-advance / click
  hero-progress-fill  5000ms   linear       slide active
  card-image-scale    300ms    ease         card hover
  card-shadow-rise    300ms    ease         card hover
  quick-view-slide    300ms    ease         card hover
  wishlist-heart-pop  300ms    spring-like  wishlist toggle
    keyframe: 0% scale(1) → 40% scale(1.35) → 70% scale(0.9) → 100% scale(1)
  dropdown-reveal     200ms    ease-out     hover/focus
    keyframe: translateY(-8px)→0 + opacity 0→1
  toast-enter         280ms    ease-out     dispatch
    keyframe: translateY(100%)→0 + opacity 0→1
  toast-exit          200ms    ease-in      auto-dismiss / close
    keyframe: opacity 1→0 + translateY(0→8px)
  button-press        100ms    ease         :active
    keyframe: scale(1)→scale(0.97)
  nav-compress        200ms    ease         100px scroll
    keyframe: h-16 → h-[52px]
  skeleton-shimmer    1.5s     linear       always (loop)
  page-fade-in        200ms    ease         route change
    keyframe: opacity(0→1)
  filter-expand       200ms    ease-out     click group header
    keyframe: max-height 0→contentHeight
  back-to-top-appear  300ms    ease         400px scroll
    keyframe: opacity(0→1) + translateY(8px→0)
  promo-banner-zoom   300ms    ease         card hover
    keyframe: scale(1.03) on inner image

Reduced-motion:
  @media (prefers-reduced-motion: reduce) — disable all except opacity transitions
```

---

## 7. Responsive Behaviour

```
Desktop (≥1024px):
  - Full mega-menu on hover
  - 4-column product grids
  - Side-by-side PDP (60/40)
  - Visible filter sidebar (sticky)
  - Announcement bar always visible
  - Bottom-nav hidden

Tablet (768–1023px):
  - Category nav collapses to hamburger
  - 3-column product grids
  - Filter panel → bottom sheet drawer
  - PDP: stacked (image then info)
  - Announcement bar visible

Mobile (<768px):
  - Bottom navigation bar: Home | Categories | Search | Wishlist | Profile
  - 2-column product grids
  - Full-screen search overlay on tap
  - Sticky ATC buttons on PDP (bottom)
  - Swipeable carousels and horizontal strips
  - Touch targets: min 44×44px for ALL interactive elements
  - Font sizes reduce by ~1 step (e.g. 15px → 13px)
  - No hover states — use :active and long-press patterns
```

---

## 8. Trust & Utility Elements

### 8.1 StyleNest Promise Badges

```
Location:  PDP below action buttons, Cart page header
Layout:    4-badge horizontal row (overflow-x auto on mobile)
Style:     Small icon (20px) + 2-line text, bg-light-gray pill (px-3 py-2)
Badges:
  1. Shield (Genuine Products)
  2. Truck (Free Delivery ₹499+)
  3. RotateCcw (30-day Easy Returns)
  4. Star (Quality Guaranteed)
Gap:       gap-3 (desktop), gap-2 (mobile)
```

### 8.2 Offer / Coupon Tags

```
Container: Dashed border (1px dashed var(--sn-border)), bg-light-gray, border-radius md
Icon:      Tag icon, var(--sn-red), 16px
Text:      "Use code STYLENEST10 — Extra 10% off" (13px DM Sans)
CTA:       "COPY CODE" — 12px SemiBold, var(--sn-red), tracking-wide
Action:    Copy to clipboard → button text → "COPIED ✓" (1.5s reset)
```

### 8.3 NeuCoins Loyalty Widget

```
Display:  Inline pill: gold coin SVG + "Earn X NeuCoins on this order"
Color:    var(--sn-gold)
Font:     12px DM Sans Medium
Position: Below price block on PDP, above ATC in cart
```

---

## 9. Icon System

**Library**: Lucide Icons (lucide-angular, MIT) — consistent stroke-based icons at all sizes.

```
Core icon tokens:
  --icon-xs: 14px   (meta, inline)
  --icon-sm: 16px   (form fields, badges)
  --icon-md: 20px   (trust badges, nav labels)
  --icon-lg: 24px   (nav, action buttons)
  --icon-xl: 32px   (empty state, feature tiles)
  --icon-2xl: 48px  (hero illustrations)

Required icons (Lucide names):
  Navigation:    Search, Menu, X, ChevronLeft, ChevronRight, ChevronDown, ChevronUp
  Commerce:      Heart, ShoppingBag, User, Star, Tag, Percent
  Trust:         Shield, Truck, RotateCcw, BadgeCheck
  Utility:       Copy, Share2, ZoomIn, Eye, ArrowRight, ExternalLink
  Status:        Check, AlertCircle, Info, AlertTriangle
  Social:        Instagram, Facebook, Twitter, Youtube
  Admin:         Settings, Package, Users, BarChart2, PlusCircle
```

---

## 10. Accessibility Guidelines

```
Color contrast (WCAG AA minimum):
  - Body text (#1A1A1A on #F5F5F5): 16.1:1 ✓ AAA
  - Red (#E31837) on white (#FFF): 4.6:1 ✓ AA (borderline — never use for small text)
  - Gold (#C9A84C) on navy (#1C2B4A): 4.7:1 ✓ AA
  - Mid-gray (#9E9E9E) on white: 2.85:1 ✗ — use only for decorative/non-essential

Focus management:
  - Focus ring: 2px solid var(--sn-red), 2px offset, always visible (:focus-visible)
  - Skip-to-content: visually hidden, appears on keyboard focus (position absolute)
  - Modal/drawer: focus trap + return focus on close
  - Dropdown menus: keyboard navigation (ArrowUp/Down, Escape closes)

Semantic HTML:
  - <header>, <nav>, <main>, <aside>, <footer> landmarks
  - <h1> per page (only one), logical heading hierarchy
  - ARIA labels on all icon-only buttons
  - role="dialog" + aria-labelledby on modals
  - aria-live="polite" for cart updates, loading complete
  - aria-live="assertive" for error messages
  - Product images: alt = brandName + " " + productName

Touch / Mobile:
  - Minimum 44×44px tap targets (WCAG 2.5.5)
  - No hover-only interactions (mirror with :focus/:active)
  - Swipe gestures paired with button controls (carousel prev/next)
```

---

## 11. Performance Guidelines

```
Images:
  - Always use loading="lazy" except above-fold hero image (loading="eager")
  - Explicit width + height attributes (prevents layout shift)
  - Prefer WebP format with JPEG fallback
  - Intersection Observer for fade-in on scroll

Fonts:
  - Preconnect to fonts.googleapis.com and fonts.gstatic.com
  - font-display: swap to prevent FOIT
  - Self-host in production (no external request)

Angular-specific:
  - ChangeDetectionStrategy.OnPush on every component (no exceptions)
  - No subscribe() in component classes — AsyncPipe only
  - trackBy on all @for loops (use entity id)
  - Lazy load feature modules — loadComponent() / loadChildren()
  - Signal-based state for local UI (signal(), computed())
  - NgRx only for cross-component / cross-route shared state

Bundle:
  - Route-level code splitting (already done via lazy routes)
  - No import of full icon libraries — import individual icons
  - PurgeCSS via Tailwind (production only)
```

---

## 12. Angular Implementation Patterns

```typescript
// Every component must include:
@Component({
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  // ...
})

// Local UI state — signals, not rxjs
scrolled = signal(false);
isWishlisted = signal(false);

// Template — async pipe for all store observables
{{ products$ | async }}

// Loops — always trackBy
@for (product of products; track product.id) { ... }

// Conditional rendering — @if / @for / @switch (Angular 17+ control flow)
@if (isLoggedIn$ | async) { ... }

// Never in component class:
//   ✗ this.store.select(x).subscribe(...)
//   ✗ this.httpClient.get(...)
//   ✗ any: type annotation

// Services inject via inject() (not constructor DI)
private readonly store = inject(Store);
private readonly router = inject(Router);
```

---

## 13. State Design (Loading / Error / Empty)

Every data-driven view must handle all three states:

```
Loading:  SkeletonLoaderComponent — match shape of actual content
Error:    Inline error card (red border-l-4, error icon, message + retry button)
Empty:    EmptyStateComponent — icon + title + subtitle + CTA (§4.16)
Data:     Actual content

Pattern (in template):
  @if (isLoading$ | async) {
    <app-skeleton-loader … />
  } @else if (error$ | async; as err) {
    <div role="alert" class="error-card">{{ err.message }}</div>
  } @else if ((items$ | async)?.length === 0) {
    <app-empty-state … />
  } @else {
    <!-- actual content -->
  }
```

---

## 14. Phase 9 Design Goals (Frontend Refresh)

The Phase 9 improvement sprint targets these specific UI/UX upgrades:

| Priority | Component | Improvement |
|----------|-----------|-------------|
| P1 | Hero Carousel | Slide progress bar + pause-on-hover + keyboard nav |
| P1 | Product Card | Filled-heart wishlist state + pop animation |
| P1 | Section Header | Reusable eyebrow + title + view-all component |
| P1 | Home Page | Live "New Arrivals" + "Trending Now" product sections |
| P2 | Header | Scroll-aware compression (64px → 52px), shadow on scroll |
| P2 | styles.scss | heart-pop, progress-bar, page-fade-in keyframes |
| P2 | Category Strip | Better icon containers + active state ring |
| P2 | Promo Banners | Image zoom on hover + richer overlay |
| P3 | Breadcrumb | New BreadcrumbComponent (PLP + PDP) |
| P3 | Back-to-Top | Fixed button with appear/disappear animation |
| P3 | Footer | Gold column headings, better link spacing, payment icons |

---

## 15. Implementation Checklist

Track component build status:

- [x] CSS variables & design tokens (styles.scss)
- [x] Google Fonts imported (Playfair Display + DM Sans)
- [x] Tailwind config with custom tokens and breakpoints
- [x] Announcement bar (dismissible)
- [x] Navigation (desktop category nav + mobile hamburger)
- [x] Hero carousel (auto-play, crossfade, dots)
- [x] Category shortcut strip
- [x] Promotional banners (2-up & 3-up)
- [x] Brand logo strip (grayscale → color hover)
- [x] Product card (3:4 portrait, wishlist, hover states)
- [x] Product grid (4/3/2 col responsive)
- [x] Section header component
- [x] Footer (4-column + social + payment icons)
- [x] PLP (filter sidebar + sort + grid + pagination)
- [x] PDP (image gallery + info panel + size/colour selectors + ATC)
- [x] Cart page (items + summary + coupon)
- [x] Wishlist page
- [x] Toast / snackbar notifications
- [x] Skeleton loaders
- [x] Empty state component
- [x] Responsive behaviour (mobile/tablet/desktop)
- [ ] Hero carousel progress bar + pause-on-hover
- [ ] Product card filled-heart wishlist animation
- [ ] Scroll-aware header compression
- [ ] Home page "New Arrivals" live API section
- [ ] Home page "Trending Now" section
- [ ] Breadcrumb component
- [ ] Back-to-top button
- [ ] Accessibility audit (contrast, keyboard, ARIA)
- [ ] Reduced-motion @media support

---

*This DESIGN.md is a living document. Phase and date stamp changes in git commits.*
