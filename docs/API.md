# API.md — StyleNest E-Commerce Clone
# Full Endpoint Reference — All 14 Controllers

All routes are prefixed with `/api/v1/`.  
All error responses conform to **RFC 7807 ProblemDetails** (`application/problem+json`).  
Bearer token format: `Authorization: Bearer <accessToken>`

---

## Common Error Shapes

### 400 Bad Request (FluentValidation)
```json
[
  { "propertyName": "Email", "errorMessage": "Email is required." },
  { "propertyName": "Password", "errorMessage": "Password must be at least 6 characters." }
]
```

### ProblemDetails (unhandled exceptions — 500 / 404 / 401)
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "instance": "/api/v1/products/00000000-0000-0000-0000-000000000000",
  "traceId": "00-abc123-def456-00"
}
```

---

## 1. Auth.API — `http://localhost:5001`

### `POST /api/v1/auth/register`
Register a new user account.

**Auth required:** No

**Request body:**
```json
{
  "firstName": "Riya",
  "lastName": "Sharma",
  "email": "riya@example.com",
  "password": "Secure@123",
  "confirmPassword": "Secure@123"
}
```

**Success 200:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "d2f3a1b4-...",
  "accessTokenExpiresAt": "2026-05-17T10:15:00Z",
  "user": {
    "id": "a1b2c3d4-...",
    "email": "riya@example.com",
    "firstName": "Riya",
    "lastName": "Sharma"
  }
}
```

**Error 400:** Email already registered, passwords do not match, or missing required fields.

---

### `POST /api/v1/auth/login`
Authenticate with email + password and receive JWT tokens.

**Auth required:** No

**Request body:**
```json
{
  "email": "admin@stylenest.com",
  "password": "Admin@123"
}
```

**Success 200:** Same shape as `/register` response above.

**Error 401:**
```json
{ "code": "INVALID_CREDENTIALS", "message": "Email or password is incorrect." }
```

---

### `POST /api/v1/auth/refresh`
Exchange a valid refresh token for a new access token.

**Auth required:** No

**Request body:**
```json
{ "refreshToken": "d2f3a1b4-..." }
```

**Success 200:** Same shape as `/login` response.

**Error 401:** Refresh token expired, revoked, or not found.

---

### `POST /api/v1/auth/logout`
Revoke the provided refresh token (Bearer token required).

**Auth required:** Yes (any authenticated user)

**Request body:**
```json
{ "refreshToken": "d2f3a1b4-..." }
```

**Success 204:** No content.

---

## 2. User.API — `http://localhost:5002`

All endpoints require `Authorization: Bearer <token>`.

---

### `GET /api/v1/users/me`
Get the authenticated user's profile.

**Auth required:** Yes

**Success 200:**
```json
{
  "id": "a1b2c3d4-...",
  "email": "riya@example.com",
  "firstName": "Riya",
  "lastName": "Sharma"
}
```

**Error 404:** User not found in database.

---

### `PUT /api/v1/users/me`
Update the authenticated user's profile.

**Auth required:** Yes

**Request body:**
```json
{
  "firstName": "Riya",
  "lastName": "Gupta"
}
```

**Success 200:** Updated user profile (same shape as GET above).

---

### `GET /api/v1/users/me/addresses`
List all saved addresses for the authenticated user.

**Auth required:** Yes

**Success 200:**
```json
[
  {
    "id": "b2c3d4e5-...",
    "label": "Home",
    "recipientName": "Riya Sharma",
    "phoneNumber": "9876543210",
    "addressLine1": "12 Park Street",
    "addressLine2": "Flat 3A",
    "city": "Mumbai",
    "state": "Maharashtra",
    "pinCode": "400001",
    "isDefault": true
  }
]
```

---

### `POST /api/v1/users/me/addresses`
Add a new delivery address.

**Auth required:** Yes

**Request body:**
```json
{
  "label": "Home",
  "recipientName": "Riya Sharma",
  "phoneNumber": "9876543210",
  "addressLine1": "12 Park Street",
  "addressLine2": "Flat 3A",
  "city": "Mumbai",
  "state": "Maharashtra",
  "pinCode": "400001",
  "isDefault": true
}
```

**Success 201:** Created address object (same shape as list item above).

**Error 400:** Missing required fields.

---

### `DELETE /api/v1/users/me/addresses/{id}`
Delete a saved address by its GUID.

**Auth required:** Yes

**Path param:** `id` — address GUID

**Success 204:** No content.

**Error 404:** Address not found or does not belong to the current user.

---

### `GET /api/v1/users/me/wishlist`
Get the current user's wishlist items.

**Auth required:** Yes

**Success 200:**
```json
[
  {
    "productId": "c3d4e5f6-...",
    "productName": "Floral Kurta",
    "imageUrl": "https://cdn.example.com/floral-kurta.jpg",
    "price": 1299.00
  }
]
```

---

### `POST /api/v1/users/me/wishlist/{productId}`
Add a product to the wishlist.

**Auth required:** Yes

**Path param:** `productId` — product GUID

**Success 204:** No content.

**Error 404:** Product not found.

---

### `DELETE /api/v1/users/me/wishlist/{productId}`
Remove a product from the wishlist.

**Auth required:** Yes

**Path param:** `productId` — product GUID

**Success 204:** No content.

---

## 3. Catalog.API — Products — `http://localhost:5003`

---

### `GET /api/v1/products`
List products with pagination and optional filters.

**Auth required:** No

**Query params:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number (≥ 1) |
| `pageSize` | int | 20 | Items per page (1–100) |
| `categoryId` | guid | — | Filter by category |
| `brandId` | guid | — | Filter by brand |
| `search` | string | — | Full-text search on product name |
| `sortBy` | string | — | `price_asc`, `price_desc`, `newest`, `rating` |
| `minPrice` | decimal | — | Minimum price filter |
| `maxPrice` | decimal | — | Maximum price filter |

**Success 200:**
```json
{
  "items": [
    {
      "id": "c3d4e5f6-...",
      "name": "Floral Kurta",
      "slug": "floral-kurta",
      "description": "Lightweight cotton kurta with floral print.",
      "price": 1299.00,
      "salePrice": 999.00,
      "brandId": "brand-guid-...",
      "brandName": "FabIndia",
      "categoryId": "cat-guid-...",
      "categoryName": "Kurtas",
      "imageUrls": ["https://cdn.example.com/img1.jpg"],
      "variants": [
        { "id": "var-guid-...", "size": "M", "colour": "Blue", "stockQuantity": 10, "priceOverride": null }
      ],
      "rating": 4.3,
      "reviewCount": 87,
      "inStock": true
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**Error 400:** `page < 1` or `pageSize > 100`.

---

### `GET /api/v1/products/{id}`
Get a single product by GUID.

**Auth required:** No

**Path param:** `id` — product GUID

**Success 200:** Single `ProductDto` (same shape as items array above).

**Error 404:** Product not found.

---

### `POST /api/v1/products`
Create a new product.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "name": "Floral Kurta",
  "description": "Lightweight cotton kurta with floral print.",
  "brandId": "brand-guid-...",
  "categoryId": "cat-guid-...",
  "price": 1299.00,
  "salePrice": 999.00,
  "imageUrls": ["https://cdn.example.com/img1.jpg"]
}
```

**Success 201:** Created `ProductDto`.

**Error 400:** Validation failure — name required (2–200 chars), price > 0, brandId/categoryId required.

---

### `PUT /api/v1/products/{id}`
Update an existing product.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "name": "Floral Kurta — Revised",
  "description": "Updated description.",
  "price": 1199.00,
  "salePrice": null,
  "isActive": true
}
```

**Success 200:** Updated `ProductDto`.

**Error 400:** Validation failure.  
**Error 404:** Product not found.

---

## 4. Catalog.API — Categories — `http://localhost:5003`

---

### `GET /api/v1/categories`
List all categories.

**Auth required:** No

**Success 200:**
```json
[
  {
    "id": "cat-guid-...",
    "name": "Kurtas",
    "slug": "kurtas",
    "parentId": null,
    "imageUrl": "https://cdn.example.com/kurtas.jpg"
  }
]
```

---

### `POST /api/v1/categories`
Create a new category.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "name": "Lehengas",
  "parentId": null,
  "imageUrl": "https://cdn.example.com/lehengas.jpg"
}
```

**Success 201:** Created `CategoryDto`.

**Error 400:** Name required (2–100 chars).

---

## 5. Catalog.API — Brands — `http://localhost:5003`

---

### `GET /api/v1/brands`
List all brands.

**Auth required:** No

**Success 200:**
```json
[
  {
    "id": "brand-guid-...",
    "name": "FabIndia",
    "slug": "fabindia",
    "logoUrl": "https://cdn.example.com/fabindia-logo.png"
  }
]
```

---

### `POST /api/v1/brands`
Create a new brand.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "name": "Manyavar",
  "logoUrl": "https://cdn.example.com/manyavar-logo.png"
}
```

**Success 201:** Created `BrandDto`.

**Error 400:** Name required (2–100 chars).

---

## 6. Catalog.API — Seller Products — `http://localhost:5003`

All endpoints require `Authorization: Bearer <token>` with **Seller** role.

---

### `GET /api/v1/seller/products`
List the authenticated seller's own products.

**Auth required:** Yes — **Seller** role

**Success 200:**
```json
[
  {
    "id": "prod-guid-...",
    "name": "Silk Saree",
    "price": 4500.00,
    "stockQuantity": 25,
    "isActive": true,
    "categoryName": "Sarees",
    "brandName": "TaneirA"
  }
]
```

---

### `POST /api/v1/seller/products`
Create a new product listing for the seller.

**Auth required:** Yes — **Seller** role

**Request body:**
```json
{
  "name": "Silk Saree",
  "description": "Pure silk saree with gold zari border.",
  "categoryId": "cat-guid-...",
  "brandId": "brand-guid-...",
  "price": 4500.00,
  "stockQuantity": 25,
  "imageUrls": ["https://cdn.example.com/saree1.jpg"]
}
```

**Success 201:** Created `SellerProductDto`.

**Error 400:** Validation failure.

---

### `PUT /api/v1/seller/products/{id}`
Update the seller's own product.

**Auth required:** Yes — **Seller** role

**Request body:**
```json
{
  "name": "Pure Silk Saree",
  "price": 4200.00,
  "stockQuantity": 20,
  "isActive": true
}
```

**Success 200:** Updated `SellerProductDto`.

**Error 400:** Validation failure.  
**Error 404:** Product not found or does not belong to this seller.

---

### `DELETE /api/v1/seller/products/{id}`
Delete the seller's own product.

**Auth required:** Yes — **Seller** role

**Success 204:** No content.

**Error 404:** Product not found or does not belong to this seller.

---

## 7. Cart.API — `http://localhost:5004`

All endpoints require `Authorization: Bearer <token>`.

---

### `GET /api/v1/cart`
Get the current user's cart.

**Auth required:** Yes

**Success 200:**
```json
{
  "id": "cart-guid-...",
  "items": [
    {
      "id": "item-guid-...",
      "productVariantId": "var-guid-...",
      "productId": "prod-guid-...",
      "productName": "Floral Kurta",
      "imageUrl": "https://cdn.example.com/img1.jpg",
      "size": "M",
      "colour": "Blue",
      "unitPrice": 999.00,
      "quantity": 2,
      "totalPrice": 1998.00
    }
  ],
  "subTotal": 1998.00,
  "discountAmount": 200.00,
  "total": 1798.00,
  "couponCode": "SAVE200"
}
```

---

### `POST /api/v1/cart/items`
Add a product to the cart.

**Auth required:** Yes

**Request body:**
```json
{
  "productId": "prod-guid-...",
  "size": "M",
  "colour": "Blue",
  "quantity": 2
}
```

**Success 200:** Updated `CartDto`.

**Error 400:** Validation failure — `quantity` must be ≥ 1.

---

### `PUT /api/v1/cart/items/{id}`
Update quantity of a cart item.

**Auth required:** Yes

**Path param:** `id` — cart item GUID

**Request body:**
```json
{ "quantity": 3 }
```

**Success 200:** Updated `CartDto`.

**Error 400:** Validation failure.  
**Error 404:** Cart item not found.

---

### `DELETE /api/v1/cart/items/{id}`
Remove a cart item.

**Auth required:** Yes

**Path param:** `id` — cart item GUID

**Success 200:** Updated `CartDto` (without the removed item).

**Error 404:** Cart item not found.

---

### `POST /api/v1/cart/coupon`
Apply a coupon code to the cart.

**Auth required:** Yes

**Request body:**
```json
{ "code": "SAVE200" }
```

**Success 200:** Updated `CartDto` with `discountAmount` and `couponCode` populated.

**Error 400:** Invalid or expired coupon code, or minimum order amount not met.

---

## 8. Order.API — Customer Orders — `http://localhost:5005`

All endpoints require `Authorization: Bearer <token>`.

---

### `POST /api/v1/orders`
Place an order from the current cart contents.

**Auth required:** Yes

**Request body:**
```json
{
  "addressLine1": "12 Park Street",
  "addressLine2": "Flat 3A",
  "city": "Mumbai",
  "state": "Maharashtra",
  "pincode": "400001",
  "paymentMethod": "COD",
  "couponCode": "SAVE200"
}
```

**Success 201:**
```json
{
  "id": "order-guid-...",
  "orderNumber": "TCQ-20260517-0042",
  "status": "Placed",
  "subTotal": 1998.00,
  "discountAmount": 200.00,
  "deliveryCharge": 0.00,
  "total": 1798.00,
  "couponCode": "SAVE200",
  "createdAt": "2026-05-17T08:30:00Z",
  "items": [
    {
      "id": "item-guid-...",
      "productId": "prod-guid-...",
      "productName": "Floral Kurta",
      "imageUrl": "https://cdn.example.com/img1.jpg",
      "variantDetails": "Size: M, Colour: Blue",
      "unitPrice": 999.00,
      "quantity": 2,
      "totalPrice": 1998.00
    }
  ]
}
```

**Error 400:** Cart is empty, validation failure, or invalid coupon.

---

### `POST /api/v1/orders/buy-now`
Place an immediate single-item order (bypasses cart).

**Auth required:** Yes

**Request body:**
```json
{
  "productId": "prod-guid-...",
  "size": "M",
  "colour": "Blue",
  "quantity": 1
}
```

**Success 201:** `OrderDto` (same shape as POST /orders above).

**Error 400:** Invalid product, quantity < 1, or out-of-stock.  
**Error 404:** Product not found.

---

### `GET /api/v1/orders`
List all orders for the current user.

**Auth required:** Yes

**Success 200:** Array of `OrderDto` objects (most recent first).

---

### `GET /api/v1/orders/{id}`
Get a single order by GUID.

**Auth required:** Yes

**Path param:** `id` — order GUID

**Success 200:** `OrderDto`.

**Error 404:** Order not found or does not belong to this user.

---

### `POST /api/v1/orders/{id}/cancel`
Cancel a pending order.

**Auth required:** Yes

**Path param:** `id` — order GUID

**Success 204:** No content.

**Error 400:** Order is already shipped or delivered (cannot be cancelled).  
**Error 404:** Order not found.

---

## 9. Order.API — Seller Orders — `http://localhost:5005`

---

### `GET /api/v1/seller/orders`
List all orders containing the seller's products.

**Auth required:** Yes — **Seller** role

**Success 200:**
```json
[
  {
    "orderId": "order-guid-...",
    "orderNumber": "TCQ-20260517-0042",
    "buyerEmail": "riya@example.com",
    "status": "Placed",
    "itemCount": 2,
    "totalAmount": 1798.00,
    "createdAt": "2026-05-17T08:30:00Z"
  }
]
```

---

## 10. Admin.API — Admin Orders — `http://localhost:5009`

All endpoints require `Authorization: Bearer <token>` with **Admin** role.

---

### `GET /api/v1/admin/orders`
List all orders across all users.

**Auth required:** Yes — **Admin** role

**Success 200:** Array of `AdminOrderDto`:
```json
[
  {
    "id": "order-guid-...",
    "orderNumber": "TCQ-20260517-0042",
    "userEmail": "riya@example.com",
    "totalAmount": 1798.00,
    "status": "Placed",
    "createdAt": "2026-05-17T08:30:00Z",
    "itemCount": 2
  }
]
```

---

### `PUT /api/v1/admin/orders/{id}/status`
Update the status of an order.

**Auth required:** Yes — **Admin** role

**Path param:** `id` — order GUID

**Request body:**
```json
{ "status": "Shipped" }
```

Valid status values: `Confirmed`, `Processing`, `Shipped`, `OutForDelivery`, `Delivered`, `Cancelled`

**Success 200:** Updated `AdminOrderDto`.

**Error 400:** Invalid status value.  
**Error 404:** Order not found.

---

## 11. Admin.API — Admin Products — `http://localhost:5009`

---

### `GET /api/v1/admin/products`
List all products (includes inactive).

**Auth required:** Yes — **Admin** role

**Success 200:**
```json
[
  {
    "id": "prod-guid-...",
    "name": "Floral Kurta",
    "brandName": "FabIndia",
    "categoryName": "Kurtas",
    "price": 999.00,
    "inStock": true,
    "isActive": true,
    "createdAt": "2026-05-01T00:00:00Z"
  }
]
```

---

### `PUT /api/v1/admin/products/{id}/status`
Activate or deactivate a product listing.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{ "isActive": false }
```

**Success 200:** Updated `AdminProductDto`.

**Error 400:** Missing `isActive` field.  
**Error 404:** Product not found.

---

## 12. Admin.API — Admin Users — `http://localhost:5009`

---

### `GET /api/v1/admin/users`
List all registered users and their roles.

**Auth required:** Yes — **Admin** role

**Success 200:**
```json
[
  {
    "id": "user-guid-...",
    "email": "riya@example.com",
    "firstName": "Riya",
    "lastName": "Sharma",
    "roles": ["Customer"],
    "emailConfirmed": true,
    "createdAt": "2026-05-10T12:00:00Z"
  }
]
```

---

### `POST /api/v1/admin/users/create-seller`
Create a new seller account (assigns Seller role).

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "firstName": "Ankit",
  "lastName": "Verma",
  "email": "ankit@sellershop.com",
  "password": "Seller@123"
}
```

**Success 201:**
```json
{
  "id": "user-guid-...",
  "email": "ankit@sellershop.com",
  "firstName": "Ankit",
  "lastName": "Verma"
}
```

**Error 400:** Validation failure — all fields required.  
**Error 409:** Email already registered.

---

## 13. Admin.API — Banners — `http://localhost:5009`

---

### `GET /api/v1/admin/banners`
List all banners.

**Auth required:** Yes — **Admin** role

**Success 200:**
```json
[
  {
    "id": "banner-guid-...",
    "title": "Summer Sale",
    "imageUrl": "https://cdn.example.com/summer-sale.jpg",
    "linkUrl": "/search?sale=true",
    "placement": "Hero",
    "displayOrder": 1,
    "isActive": true,
    "startsAt": "2026-06-01T00:00:00Z",
    "endsAt": "2026-06-30T23:59:59Z"
  }
]
```

`placement` values: `Hero`, `CategoryStrip`, `PromoBanner`

---

### `GET /api/v1/admin/banners/{id}`
Get a single banner by GUID.

**Auth required:** Yes — **Admin** role

**Success 200:** `BannerDto` (same shape as list item above).

**Error 404:** Banner not found.

---

### `POST /api/v1/admin/banners`
Create a new banner.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "title": "Summer Sale",
  "imageUrl": "https://cdn.example.com/summer-sale.jpg",
  "linkUrl": "/search?sale=true",
  "placement": "Hero",
  "displayOrder": 1,
  "isActive": true,
  "startsAt": "2026-06-01T00:00:00Z",
  "endsAt": "2026-06-30T23:59:59Z"
}
```

**Success 201:** Created `BannerDto`.

---

### `PUT /api/v1/admin/banners/{id}`
Update an existing banner.

**Auth required:** Yes — **Admin** role

**Request body:** Same shape as POST (all fields).

**Success 200:** Updated `BannerDto`.

**Error 404:** Banner not found.

---

### `DELETE /api/v1/admin/banners/{id}`
Delete a banner.

**Auth required:** Yes — **Admin** role

**Success 204:** No content.

**Error 404:** Banner not found.

---

## 14. Admin.API — Coupons — `http://localhost:5009`

---

### `GET /api/v1/admin/coupons`
List all coupons.

**Auth required:** Yes — **Admin** role

**Success 200:**
```json
[
  {
    "id": "coupon-guid-...",
    "code": "SAVE200",
    "description": "Flat ₹200 off on orders above ₹999",
    "discountType": "Flat",
    "discountValue": 200.00,
    "minOrderAmount": 999.00,
    "maxDiscountCap": null,
    "usageLimitPerUser": 1,
    "totalUsageLimit": 500,
    "usedCount": 37,
    "isActive": true,
    "startsAt": "2026-05-01T00:00:00Z",
    "expiresAt": "2026-05-31T23:59:59Z"
  }
]
```

`discountType` values: `Flat`, `Percentage`

---

### `GET /api/v1/admin/coupons/{id}`
Get a single coupon by GUID.

**Auth required:** Yes — **Admin** role

**Success 200:** `CouponDto` (same shape as list item above).

**Error 404:** Coupon not found.

---

### `POST /api/v1/admin/coupons`
Create a new coupon.

**Auth required:** Yes — **Admin** role

**Request body:**
```json
{
  "code": "SUMMER10",
  "description": "10% off on all summer wear",
  "discountType": "Percentage",
  "discountValue": 10.0,
  "minOrderAmount": 499.00,
  "maxDiscountCap": 300.00,
  "usageLimitPerUser": 2,
  "totalUsageLimit": 1000,
  "isActive": true,
  "startsAt": "2026-06-01T00:00:00Z",
  "expiresAt": "2026-06-30T23:59:59Z"
}
```

**Success 201:** Created `CouponDto`.

**Error 400:** Missing required fields.

---

### `PUT /api/v1/admin/coupons/{id}`
Update an existing coupon (code cannot be changed).

**Auth required:** Yes — **Admin** role

**Request body:** Same as POST but without `code` field.

**Success 200:** Updated `CouponDto`.

**Error 404:** Coupon not found.

---

### `DELETE /api/v1/admin/coupons/{id}`
Delete a coupon.

**Auth required:** Yes — **Admin** role

**Success 204:** No content.

**Error 404:** Coupon not found.

---

## Summary Table

| # | Controller | Service | Endpoints | Auth |
|---|-----------|---------|-----------|------|
| 1 | AuthController | Auth.API :5001 | POST register, login, refresh, logout | Mixed |
| 2 | UsersController | User.API :5002 | GET/PUT me, CRUD addresses, CRUD wishlist | All auth |
| 3 | ProductsController | Catalog.API :5003 | GET list (paginated), GET by id, POST, PUT | GET = public |
| 4 | CategoriesController | Catalog.API :5003 | GET list, POST | GET = public |
| 5 | BrandsController | Catalog.API :5003 | GET list, POST | GET = public |
| 6 | SellerProductsController | Catalog.API :5003 | GET list, POST, PUT, DELETE | Seller role |
| 7 | CartController | Cart.API :5004 | GET cart, POST/PUT/DELETE items, POST coupon | Auth |
| 8 | OrdersController | Order.API :5005 | POST order, POST buy-now, GET list, GET by id, POST cancel | Auth |
| 9 | SellerOrdersController | Order.API :5005 | GET seller orders | Seller role |
| 10 | AdminOrdersController | Admin.API :5009 | GET all orders, PUT status | Admin role |
| 11 | AdminProductsController | Admin.API :5009 | GET all products, PUT status | Admin role |
| 12 | AdminUsersController | Admin.API :5009 | GET all users, POST create-seller | Admin role |
| 13 | BannersController | Admin.API :5009 | Full CRUD (GET list, GET by id, POST, PUT, DELETE) | Admin role |
| 14 | CouponsController | Admin.API :5009 | Full CRUD (GET list, GET by id, POST, PUT, DELETE) | Admin role |
