# DATABASE_SCHEMA.md — Fashion eCommerce Platform
> Complete SQL Server schema reference. All tables, columns, constraints, and indexes.
> Engine: SQL Server 2022 · ORM: EF Core 9 (Code-First) · Migration prefix: `Phase<N>_<Schema>_<Change>`

---

## Schema Overview

| Schema | Domain | Tables |
|---|---|---|
| `[auth]` | Authentication & Identity | Users, Roles, RefreshTokens, OtpCodes, Addresses |
| `[catalog]` | Product Catalog | Categories, Brands, AttributeDefinitions, CategoryAttributes, Products, ProductAttributes, ProductVariants, ProductVariantOptions, ProductImages, ProductVideos, ProductReviews |
| `[commerce]` | Shopping | Carts, CartItems, SavedForLater, Wishlists |
| `[orders]` | Order Lifecycle | Orders, OrderItems, OrderStatusHistory, Returns |
| `[payments]` | Payments | Payments, Refunds |
| `[seller]` | Seller Management | Sellers, SellerInventory, SellerPayouts |
| `[admin]` | Administration | Banners, Coupons, CouponUsages, CmsPages, AuditLogs |
| `[analytics]` | Reporting | DailyRevenue, ProductViews, SearchTerms |
| `[wallet]` | Wallet | Wallets, WalletTransactions |
| `[media]` | Media Storage | MediaFiles |
| `[notifications]` | Notifications | NotificationTemplates, NotificationLogs |

---

## [auth] Schema

### AspNetUsers (extended by Identity)

```sql
-- ASP.NET Core Identity base + custom columns
Id              NVARCHAR(450) PK
UserName        NVARCHAR(256) NOT NULL
NormalizedUserName  NVARCHAR(256) UNIQUE
Email           NVARCHAR(256) NOT NULL
NormalizedEmail NVARCHAR(256) UNIQUE
EmailConfirmed  BIT NOT NULL DEFAULT 0
PasswordHash    NVARCHAR(MAX)
PhoneNumber     NVARCHAR(MAX)
PhoneNumberConfirmed BIT DEFAULT 0
-- Custom columns:
FirstName       NVARCHAR(100) NOT NULL
LastName        NVARCHAR(100) NOT NULL
AvatarUrl       NVARCHAR(500)
IsActive        BIT NOT NULL DEFAULT 1
CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
IsDeleted       BIT NOT NULL DEFAULT 0
```

### RefreshTokens

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Token           NVARCHAR(500) NOT NULL UNIQUE
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
ExpiresAt       DATETIME2 NOT NULL
IsRevoked       BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()

INDEX IX_RefreshTokens_UserId ON (UserId)
INDEX IX_RefreshTokens_Token ON (Token)
```

### OtpCodes

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
Code            NVARCHAR(10) NOT NULL
Purpose         NVARCHAR(50) NOT NULL   -- 'EmailVerification' | 'PasswordReset' | 'Login'
ExpiresAt       DATETIME2 NOT NULL
IsUsed          BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL

INDEX IX_OtpCodes_UserId ON (UserId)
```

### Addresses

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
Label           NVARCHAR(50)            -- 'Home' | 'Work' | 'Other'
RecipientName   NVARCHAR(200) NOT NULL
PhoneNumber     NVARCHAR(20) NOT NULL
AddressLine1    NVARCHAR(300) NOT NULL
AddressLine2    NVARCHAR(300)
City            NVARCHAR(100) NOT NULL
State           NVARCHAR(100) NOT NULL
PinCode         NVARCHAR(10) NOT NULL
Country         NVARCHAR(50) NOT NULL DEFAULT 'India'
IsDefault       BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0

INDEX IX_Addresses_UserId ON (UserId)
```

---

## [catalog] Schema

### Categories

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Name            NVARCHAR(100) NOT NULL
Slug            NVARCHAR(120) NOT NULL UNIQUE
ParentId        UNIQUEIDENTIFIER NULL  FK → Categories(Id) -- Self-referential
ImageUrl        NVARCHAR(500)
DisplayOrder    INT NOT NULL DEFAULT 0
IsActive        BIT NOT NULL DEFAULT 1
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0

INDEX IX_Categories_ParentId ON (ParentId)
INDEX IX_Categories_Slug ON (Slug)
```

### Brands

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Name            NVARCHAR(100) NOT NULL
Slug            NVARCHAR(120) NOT NULL UNIQUE
LogoUrl         NVARCHAR(500)
IsActive        BIT NOT NULL DEFAULT 1
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0
```

### AttributeDefinitions (Dynamic EAV — Master)

```sql
Id              INT IDENTITY PK
Name            NVARCHAR(100) NOT NULL    -- 'Fabric', 'Fit', 'Shoe Size'
DataType        NVARCHAR(20) NOT NULL     -- 'string' | 'number' | 'boolean'
InputType       NVARCHAR(30) NOT NULL     -- 'text' | 'select' | 'multi-select' | 'color-picker' | 'number'
Options         NVARCHAR(MAX)             -- JSON array: ["Cotton","Polyester","Silk"]
Unit            NVARCHAR(20)              -- 'cm' | 'kg' (for number types)
IsFilterable    BIT NOT NULL DEFAULT 1    -- Show as filter on PLP
SortOrder       INT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL
```

**Seed data examples:**

| Id | Name | DataType | InputType | Options |
|---|---|---|---|---|
| 1 | Brand | string | text | null |
| 2 | Fabric | string | select | ["Cotton","Polyester","Silk","Denim","Linen"] |
| 3 | Fit | string | select | ["Slim","Regular","Loose","Oversized"] |
| 4 | Sleeve Type | string | select | ["Full Sleeve","Half Sleeve","Sleeveless","3/4 Sleeve"] |
| 5 | Neck Type | string | select | ["Round","V-Neck","Collar","Hooded"] |
| 6 | Color | string | color-picker | null |
| 7 | Clothing Size | string | multi-select | ["XS","S","M","L","XL","XXL","XXXL"] |
| 8 | Shoe Size | number | multi-select | [6,7,8,9,10,11,12] |
| 9 | Sole Material | string | select | ["Rubber","Leather","TPR","EVA"] |
| 10 | Heel Type | string | select | ["Flat","Block","Stiletto","Wedge","Kitten"] |
| 11 | Metal Type | string | select | ["Gold","Silver","Platinum","Rose Gold"] |
| 12 | Stone Type | string | select | ["Diamond","Ruby","Emerald","Pearl","None"] |
| 13 | Occasion | string | multi-select | ["Casual","Formal","Wedding","Party","Sports"] |
| 14 | Gender | string | select | ["Men","Women","Unisex","Boys","Girls"] |

### CategoryAttributes (EAV — Category to Attribute Mapping)

```sql
CategoryId      UNIQUEIDENTIFIER NOT NULL  FK → Categories(Id)
AttributeId     INT NOT NULL               FK → AttributeDefinitions(Id)
IsRequired      BIT NOT NULL DEFAULT 0
SortOrder       INT NOT NULL DEFAULT 0

PK (CategoryId, AttributeId)
```

### Products

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
SellerId        UNIQUEIDENTIFIER NOT NULL  FK → [seller].Sellers(Id)
CategoryId      UNIQUEIDENTIFIER NOT NULL  FK → Categories(Id)
BrandId         UNIQUEIDENTIFIER NOT NULL  FK → Brands(Id)
Title           NVARCHAR(300) NOT NULL
Slug            NVARCHAR(350) NOT NULL UNIQUE
Description     NVARCHAR(MAX)
BasePrice       DECIMAL(18,2) NOT NULL
MRP             DECIMAL(18,2) NOT NULL
AverageRating   DECIMAL(3,2) NOT NULL DEFAULT 0
ReviewCount     INT NOT NULL DEFAULT 0
IsActive        BIT NOT NULL DEFAULT 1
IsApproved      BIT NOT NULL DEFAULT 0     -- Admin must approve seller products
RejectionReason NVARCHAR(500)
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0

INDEX IX_Products_CategoryId_IsActive_IsApproved ON (CategoryId, IsActive, IsApproved)
INDEX IX_Products_SellerId ON (SellerId)
INDEX IX_Products_BrandId ON (BrandId)
INDEX IX_Products_Slug ON (Slug)
INDEX IX_Products_BasePrice ON (BasePrice)
FULLTEXT INDEX ON (Title, Description)  -- For search
```

### ProductAttributes (EAV — Product Attribute Values)

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → Products(Id)
AttributeId     INT NOT NULL               FK → AttributeDefinitions(Id)
Value           NVARCHAR(500) NOT NULL

INDEX IX_ProductAttributes_ProductId ON (ProductId)
UNIQUE (ProductId, AttributeId)
```

### ProductVariants

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → Products(Id)
SKU             NVARCHAR(100) NOT NULL UNIQUE
PriceOverride   DECIMAL(18,2)             -- NULL = use Product.BasePrice
IsActive        BIT NOT NULL DEFAULT 1
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL

INDEX IX_ProductVariants_ProductId ON (ProductId)
INDEX IX_ProductVariants_SKU ON (SKU)
```

### ProductVariantOptions (Which attribute values define this variant)

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
VariantId       UNIQUEIDENTIFIER NOT NULL  FK → ProductVariants(Id)
AttributeId     INT NOT NULL               FK → AttributeDefinitions(Id)
Value           NVARCHAR(100) NOT NULL     -- 'Red', 'L', '9', etc.

INDEX IX_ProductVariantOptions_VariantId ON (VariantId)
UNIQUE (VariantId, AttributeId)
```

**Example — Men's Denim Shirt:**
```
ProductVariants:
  Variant 1: SKU=SHIRT-WHT-S
  Variant 2: SKU=SHIRT-WHT-M
  Variant 3: SKU=SHIRT-BLU-L

ProductVariantOptions for Variant 1:
  AttributeId=6 (Color), Value='White'
  AttributeId=7 (Clothing Size), Value='S'
```

### ProductImages

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → Products(Id)
VariantId       UNIQUEIDENTIFIER NULL      FK → ProductVariants(Id)  -- variant-specific image
MediaFileId     UNIQUEIDENTIFIER NOT NULL  FK → [media].MediaFiles(Id)
Url             NVARCHAR(500) NOT NULL
SortOrder       INT NOT NULL DEFAULT 0
IsPrimary       BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL

INDEX IX_ProductImages_ProductId ON (ProductId)
```

### ProductVideos

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → Products(Id)
MediaFileId     UNIQUEIDENTIFIER NOT NULL  FK → [media].MediaFiles(Id)
Url             NVARCHAR(500) NOT NULL
ThumbnailUrl    NVARCHAR(500)
CreatedAt       DATETIME2 NOT NULL
```

### ProductReviews

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → Products(Id)
UserId          NVARCHAR(450) NOT NULL     FK → AspNetUsers(Id)
OrderItemId     UNIQUEIDENTIFIER NULL      FK → [orders].OrderItems(Id)  -- verified purchase
Rating          TINYINT NOT NULL           CHECK (Rating BETWEEN 1 AND 5)
Title           NVARCHAR(200)
Body            NVARCHAR(2000)
IsApproved      BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0

INDEX IX_ProductReviews_ProductId ON (ProductId)
INDEX IX_ProductReviews_UserId ON (UserId)
UNIQUE (ProductId, UserId)               -- One review per product per user
```

---

## [commerce] Schema

### Carts

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL UNIQUE  FK → AspNetUsers(Id)
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
```

### CartItems

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
CartId          UNIQUEIDENTIFIER NOT NULL  FK → Carts(Id)
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → [catalog].Products(Id)
VariantId       UNIQUEIDENTIFIER NOT NULL  FK → [catalog].ProductVariants(Id)
Quantity        INT NOT NULL               CHECK (Quantity >= 1)
PriceAtAdd      DECIMAL(18,2) NOT NULL     -- Snapshot price at time of add
AddedAt         DATETIME2 NOT NULL

INDEX IX_CartItems_CartId ON (CartId)
UNIQUE (CartId, VariantId)               -- One entry per variant per cart
```

### SavedForLater

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
ProductId       UNIQUEIDENTIFIER NOT NULL
VariantId       UNIQUEIDENTIFIER NOT NULL
SavedAt         DATETIME2 NOT NULL

INDEX IX_SavedForLater_UserId ON (UserId)
```

### Wishlists

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → [catalog].Products(Id)
VariantId       UNIQUEIDENTIFIER NULL
AddedAt         DATETIME2 NOT NULL

INDEX IX_Wishlists_UserId ON (UserId)
UNIQUE (UserId, ProductId)
```

---

## [orders] Schema

### Orders

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
OrderNumber     NVARCHAR(30) NOT NULL UNIQUE    -- 'TCQ-20260517-0042'
UserId          NVARCHAR(450) NOT NULL  FK → AspNetUsers(Id)
AddressId       UNIQUEIDENTIFIER NOT NULL  FK → [auth].Addresses(Id)
Status          NVARCHAR(30) NOT NULL DEFAULT 'Placed'
  -- Placed | Confirmed | Processing | Shipped | OutForDelivery | Delivered | Cancelled | Returned
SubTotal        DECIMAL(18,2) NOT NULL
DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0
ShippingFee     DECIMAL(18,2) NOT NULL DEFAULT 0
Total           DECIMAL(18,2) NOT NULL
CouponCode      NVARCHAR(50)
PaymentMethod   NVARCHAR(30) NOT NULL       -- COD | Wallet | Card | UPI
PlacedAt        DATETIME2 NOT NULL
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0

INDEX IX_Orders_UserId ON (UserId)
INDEX IX_Orders_Status ON (Status)
INDEX IX_Orders_PlacedAt ON (PlacedAt)
```

### OrderItems

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
OrderId         UNIQUEIDENTIFIER NOT NULL  FK → Orders(Id)
SellerId        UNIQUEIDENTIFIER NOT NULL  FK → [seller].Sellers(Id)
ProductId       UNIQUEIDENTIFIER NOT NULL
VariantId       UNIQUEIDENTIFIER NOT NULL
ProductTitle    NVARCHAR(300) NOT NULL     -- Snapshot at order time
VariantDetails  NVARCHAR(200)             -- 'Color: Red, Size: L'
ProductImageUrl NVARCHAR(500)
Quantity        INT NOT NULL
UnitPrice       DECIMAL(18,2) NOT NULL    -- Snapshot at order time
Total           DECIMAL(18,2) NOT NULL

INDEX IX_OrderItems_OrderId ON (OrderId)
INDEX IX_OrderItems_SellerId ON (SellerId)
```

### OrderStatusHistory

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
OrderId         UNIQUEIDENTIFIER NOT NULL  FK → Orders(Id)
Status          NVARCHAR(30) NOT NULL
Note            NVARCHAR(500)
ChangedByUserId NVARCHAR(450)
ChangedAt       DATETIME2 NOT NULL

INDEX IX_OrderStatusHistory_OrderId ON (OrderId)
```

### Returns

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
OrderItemId     UNIQUEIDENTIFIER NOT NULL  FK → OrderItems(Id)
Reason          NVARCHAR(500) NOT NULL
Status          NVARCHAR(30) NOT NULL DEFAULT 'Requested'
  -- Requested | Approved | PickedUp | Refunded | Rejected
RefundAmount    DECIMAL(18,2)
RequestedAt     DATETIME2 NOT NULL
ResolvedAt      DATETIME2

INDEX IX_Returns_OrderItemId ON (OrderItemId)
```

---

## [payments] Schema

### Payments

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
OrderId         UNIQUEIDENTIFIER NOT NULL  FK → [orders].Orders(Id)
Gateway         NVARCHAR(30) NOT NULL      -- 'Razorpay' | 'COD' | 'Wallet'
TransactionId   NVARCHAR(200)
Amount          DECIMAL(18,2) NOT NULL
Status          NVARCHAR(20) NOT NULL      -- 'Pending' | 'Paid' | 'Failed' | 'Refunded'
PaidAt          DATETIME2
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
```

### Refunds

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
PaymentId       UNIQUEIDENTIFIER NOT NULL  FK → Payments(Id)
Amount          DECIMAL(18,2) NOT NULL
Reason          NVARCHAR(500)
GatewayRefundId NVARCHAR(200)
Status          NVARCHAR(20) NOT NULL      -- 'Pending' | 'Processed' | 'Failed'
ProcessedAt     DATETIME2
CreatedAt       DATETIME2 NOT NULL
```

---

## [seller] Schema

### Sellers

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL UNIQUE  FK → AspNetUsers(Id)
StoreName       NVARCHAR(200) NOT NULL
GSTIN           NVARCHAR(20)
PAN             NVARCHAR(15)
BankAccountNumber NVARCHAR(30)
BankIFSC        NVARCHAR(15)
BankAccountName NVARCHAR(200)
IsVerified      BIT NOT NULL DEFAULT 0
Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending'
  -- Pending | Active | Suspended | Rejected
RejectionReason NVARCHAR(500)
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0
```

### SellerInventory

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
SellerId        UNIQUEIDENTIFIER NOT NULL  FK → Sellers(Id)
VariantId       UNIQUEIDENTIFIER NOT NULL  FK → [catalog].ProductVariants(Id)
Stock           INT NOT NULL DEFAULT 0     CHECK (Stock >= 0)
Reserved        INT NOT NULL DEFAULT 0     -- Items in active carts
LowStockThreshold INT NOT NULL DEFAULT 5
UpdatedAt       DATETIME2 NOT NULL

UNIQUE (SellerId, VariantId)
INDEX IX_SellerInventory_SellerId ON (SellerId)
```

**Business Rule:** `Available = Stock - Reserved`. Cannot go negative. Enforced via DB constraint and application logic.

### SellerPayouts

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
SellerId        UNIQUEIDENTIFIER NOT NULL  FK → Sellers(Id)
Amount          DECIMAL(18,2) NOT NULL
Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending'  -- Pending | Processed | Failed
PeriodFrom      DATETIME2 NOT NULL
PeriodTo        DATETIME2 NOT NULL
ProcessedAt     DATETIME2
CreatedAt       DATETIME2 NOT NULL

INDEX IX_SellerPayouts_SellerId ON (SellerId)
```

---

## [admin] Schema

### Banners

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Title           NVARCHAR(200) NOT NULL
ImageUrl        NVARCHAR(500) NOT NULL
LinkUrl         NVARCHAR(500)
Position        NVARCHAR(30) NOT NULL  -- 'Hero' | 'CategoryStrip' | 'PromoBanner'
DisplayOrder    INT NOT NULL DEFAULT 0
IsActive        BIT NOT NULL DEFAULT 1
StartDate       DATETIME2
EndDate         DATETIME2
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0
```

### Coupons

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Code            NVARCHAR(50) NOT NULL UNIQUE
Description     NVARCHAR(300)
Type            NVARCHAR(20) NOT NULL    -- 'Percentage' | 'Fixed' | 'FreeShipping'
Value           DECIMAL(18,2) NOT NULL
MinOrderValue   DECIMAL(18,2) NOT NULL DEFAULT 0
MaxDiscount     DECIMAL(18,2)            -- Cap for Percentage coupons
UsageLimit      INT NOT NULL DEFAULT 0   -- 0 = unlimited
PerUserLimit    INT NOT NULL DEFAULT 1
UsedCount       INT NOT NULL DEFAULT 0
IsActive        BIT NOT NULL DEFAULT 1
StartsAt        DATETIME2
ExpiresAt       DATETIME2
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
IsDeleted       BIT NOT NULL DEFAULT 0
```

### CouponUsages

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
CouponId        UNIQUEIDENTIFIER NOT NULL  FK → Coupons(Id)
UserId          NVARCHAR(450) NOT NULL
OrderId         UNIQUEIDENTIFIER NOT NULL
UsedAt          DATETIME2 NOT NULL

INDEX IX_CouponUsages_CouponId ON (CouponId)
INDEX IX_CouponUsages_UserId ON (UserId)
```

### CmsPages

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
Slug            NVARCHAR(200) NOT NULL UNIQUE
Title           NVARCHAR(300) NOT NULL
Content         NVARCHAR(MAX)
MetaDescription NVARCHAR(500)
IsPublished     BIT NOT NULL DEFAULT 0
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
```

### AuditLogs

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL
Action          NVARCHAR(100) NOT NULL   -- 'ProductApproved', 'UserBanned', etc.
Resource        NVARCHAR(100) NOT NULL   -- 'Product', 'User', 'Coupon'
ResourceId      NVARCHAR(450)
OldValues       NVARCHAR(MAX)            -- JSON snapshot before
NewValues       NVARCHAR(MAX)            -- JSON snapshot after
IpAddress       NVARCHAR(50)
UserAgent       NVARCHAR(500)
CreatedAt       DATETIME2 NOT NULL

INDEX IX_AuditLogs_UserId ON (UserId)
INDEX IX_AuditLogs_CreatedAt ON (CreatedAt)
INDEX IX_AuditLogs_Resource ON (Resource)
```

---

## [analytics] Schema

### DailyRevenue

```sql
Date            DATE PK
TotalOrders     INT NOT NULL DEFAULT 0
TotalRevenue    DECIMAL(18,2) NOT NULL DEFAULT 0
NewUsers        INT NOT NULL DEFAULT 0
CancelledOrders INT NOT NULL DEFAULT 0
```

### ProductViews

```sql
ProductId       UNIQUEIDENTIFIER NOT NULL  FK → [catalog].Products(Id)
Date            DATE NOT NULL
ViewCount       INT NOT NULL DEFAULT 0

PK (ProductId, Date)
```

### SearchTerms

```sql
Term            NVARCHAR(200) NOT NULL
Date            DATE NOT NULL
Count           INT NOT NULL DEFAULT 0

PK (Term, Date)
```

---

## [wallet] Schema

### Wallets

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL UNIQUE  FK → AspNetUsers(Id)
Balance         DECIMAL(18,2) NOT NULL DEFAULT 0  CHECK (Balance >= 0)
Currency        NVARCHAR(5) NOT NULL DEFAULT 'INR'
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
```

### WalletTransactions

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
WalletId        UNIQUEIDENTIFIER NOT NULL  FK → Wallets(Id)
Amount          DECIMAL(18,2) NOT NULL
Type            NVARCHAR(20) NOT NULL  -- 'Credit' | 'Debit' | 'Refund' | 'Cashback'
Reference       NVARCHAR(450)          -- OrderId | RefundId | PromotionId
Note            NVARCHAR(300)
CreatedAt       DATETIME2 NOT NULL

INDEX IX_WalletTransactions_WalletId ON (WalletId)
INDEX IX_WalletTransactions_CreatedAt ON (CreatedAt)
```

---

## [media] Schema

### MediaFiles

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
FileName        NVARCHAR(300) NOT NULL
OriginalName    NVARCHAR(300) NOT NULL
StoragePath     NVARCHAR(500) NOT NULL   -- MinIO/Blob path
BucketName      NVARCHAR(100) NOT NULL   -- 'products' | 'banners' | 'avatars'
MimeType        NVARCHAR(100) NOT NULL
SizeBytes       BIGINT NOT NULL
ThumbUrl        NVARCHAR(500)            -- 150×150
CardUrl         NVARCHAR(500)            -- 400×400
FullUrl         NVARCHAR(500)            -- 800×800
IsProcessed     BIT NOT NULL DEFAULT 0   -- Image resize complete
UploadedByUserId NVARCHAR(450) NOT NULL
CreatedAt       DATETIME2 NOT NULL
```

---

## [notifications] Schema

### NotificationTemplates

```sql
Id              INT IDENTITY PK
Name            NVARCHAR(100) NOT NULL UNIQUE   -- 'OrderPlaced', 'WelcomeEmail'
Channel         NVARCHAR(20) NOT NULL            -- 'Email' | 'InApp' | 'Push'
Subject         NVARCHAR(300)
Body            NVARCHAR(MAX)                    -- Handlebars / Liquid template
CreatedAt       DATETIME2 NOT NULL
UpdatedAt       DATETIME2 NOT NULL
```

### NotificationLogs

```sql
Id              UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
UserId          NVARCHAR(450) NOT NULL
TemplateId      INT NOT NULL  FK → NotificationTemplates(Id)
Channel         NVARCHAR(20) NOT NULL
Subject         NVARCHAR(300)
Status          NVARCHAR(20) NOT NULL  -- 'Sent' | 'Failed' | 'Read'
IsRead          BIT NOT NULL DEFAULT 0
SentAt          DATETIME2 NOT NULL

INDEX IX_NotificationLogs_UserId ON (UserId)
INDEX IX_NotificationLogs_IsRead ON (UserId, IsRead)
```

---

## Migration Naming Convention

```
dotnet ef migrations add Phase<N>_<Schema>_<Change>

Examples:
  Phase9_Auth_AddOtpCodes
  Phase9_Catalog_AddAttributeDefinitions
  Phase9_Catalog_AddProductAttributes
  Phase9_Seller_AddSellerInventory
  Phase9_Wallet_Initial
  Phase9_Media_AddMediaFiles
  Phase9_Analytics_Initial
  Phase9_Notifications_Initial
```

---

## EF Core Entity Configuration Notes

- All entities inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt, IsDeleted)
- Global query filter: `.HasQueryFilter(e => !e.IsDeleted)` on all soft-delete entities
- `SaveChangesInterceptor` auto-sets `CreatedAt`/`UpdatedAt` before every save
- `DECIMAL(18,2)` for all money columns — never use `float` or `double`
- `NEWSEQUENTIALID()` default for all GUIDs (better index performance than `NEWID()`)
- `DATETIME2` for all date/time columns (higher precision than `DATETIME`)

---

*Cross-reference [SEEDER.md](SEEDER.md) for seed data. Updated after each schema migration.*
