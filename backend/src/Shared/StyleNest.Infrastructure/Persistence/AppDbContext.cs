using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Commerce;
using StyleNest.Infrastructure.Entities.Media;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Entities.Wallet;

namespace StyleNest.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();
    public DbSet<PincodeServiceability>  PincodeServiceabilities => Set<PincodeServiceability>();
    // ENH-CAT-008 — Category slug rename history (301-redirect support)
    public DbSet<CategorySlugHistory>    CategorySlugHistories   => Set<CategorySlugHistory>();
    public DbSet<FlashSale>     FlashSales     => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();
    public DbSet<SeoMetadata>   SeoMetadata    => Set<SeoMetadata>();
    // ENH-PDP-004 — Q&A Section
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
    public DbSet<ProductAnswer>   ProductAnswers   => Set<ProductAnswer>();
    // ENH-PDP-006 — Back-in-Stock Subscriptions
    public DbSet<BackInStockSubscription> BackInStockSubscriptions => Set<BackInStockSubscription>();
    // ENH-PDP-003 — Size Guide Modal
    public DbSet<SizeGuide> SizeGuides => Set<SizeGuide>();

    // Commerce
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    // Payments
    public DbSet<Payment>          Payments          => Set<Payment>();
    // ENH-PAY-006 — Razorpay vault tokens (no PAN stored)
    public DbSet<CardToken>        CardTokens        => Set<CardToken>();
    // ENH-PAY-004 — Durable idempotency-key records with composite covering index
    public DbSet<IdempotencyKey>   IdempotencyKeys   => Set<IdempotencyKey>();

    // Admin
    public DbSet<Banner>   Banners   => Set<Banner>();
    public DbSet<Coupon>   Coupons   => Set<Coupon>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Seller
    public DbSet<Entities.Seller.Seller> Sellers => Set<Entities.Seller.Seller>();
    public DbSet<SellerInventory> SellerInventories => Set<SellerInventory>();
    public DbSet<SellerPayout> SellerPayouts => Set<SellerPayout>();
    // ENH-SELL-002 — KYC document submissions and review workflow
    public DbSet<SellerKycDocument> SellerKycDocuments => Set<SellerKycDocument>();

    // Media
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    // Wallet
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    // Analytics
    public DbSet<DailyRevenue> DailyRevenues => Set<DailyRevenue>();
    public DbSet<ProductView> ProductViews => Set<ProductView>();
    public DbSet<SearchTerm> SearchTerms => Set<SearchTerm>();
    // ENH-SRCH-003 — Search Synonyms Dictionary
    public DbSet<SearchSynonym> SearchSynonyms => Set<SearchSynonym>();

    // Notifications
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<FcmDeviceToken> FcmDeviceTokens => Set<FcmDeviceToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Auth schema
        builder.Entity<ApplicationUser>().ToTable("Users", "auth");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles", "auth");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "auth");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "auth");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "auth");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "auth");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "auth");
        builder.Entity<RefreshToken>().ToTable("RefreshTokens", "auth");
        builder.Entity<UserAddress>().ToTable("Addresses", "auth");
        builder.Entity<OtpCode>().ToTable("OtpCodes", "auth");

        // Catalog schema
        builder.Entity<Category>().ToTable("Categories", "catalog");
        builder.Entity<Brand>().ToTable("Brands", "catalog");
        builder.Entity<Product>().ToTable("Products", "catalog");
        builder.Entity<ProductVariant>().ToTable("ProductVariants", "catalog");
        builder.Entity<ProductImage>().ToTable("ProductImages", "catalog");
        builder.Entity<Review>().ToTable("Reviews", "catalog");
        builder.Entity<AttributeDefinition>().ToTable("AttributeDefinitions", "catalog");
        builder.Entity<CategoryAttribute>().ToTable("CategoryAttributes", "catalog");
        builder.Entity<ProductAttribute>().ToTable("ProductAttributes", "catalog");
        builder.Entity<ProductVariantOption>().ToTable("ProductVariantOptions", "catalog");
        builder.Entity<PincodeServiceability>(e =>
        {
            e.ToTable("PincodeServiceabilities", "catalog");
            e.Property(p => p.Pincode).HasMaxLength(10).IsRequired();
            e.Property(p => p.City).HasMaxLength(100);
            e.Property(p => p.FreeDeliveryThreshold).HasColumnType("decimal(10,2)");
            e.HasIndex(p => p.Pincode).IsUnique();
        });

        // ENH-CAT-002 — Flash Sale schema
        builder.Entity<FlashSale>(e =>
        {
            e.ToTable("FlashSales", "catalog");
            e.Property(f => f.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(f => new { f.Status, f.EndsAt });
        });
        builder.Entity<FlashSaleItem>(e =>
        {
            e.ToTable("FlashSaleItems", "catalog");
            e.Property(fi => fi.SalePrice).HasColumnType("decimal(18,2)");
            e.Property(fi => fi.OriginalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(fi => fi.FlashSale)
             .WithMany(fs => fs.Items)
             .HasForeignKey(fi => fi.FlashSaleId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(fi => fi.Product)
             .WithMany()
             .HasForeignKey(fi => fi.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(fi => new { fi.FlashSaleId, fi.ProductId }).IsUnique();
        });

        // ENH-CAT-008 — Category slug rename history
        builder.Entity<CategorySlugHistory>(e =>
        {
            e.ToTable("CategorySlugHistory", "catalog");
            e.Property(h => h.OldSlug).HasMaxLength(300).IsRequired();
            e.Property(h => h.NewSlug).HasMaxLength(300).IsRequired();
            // Primary lookup: find a category by its old slug
            e.HasIndex(h => h.OldSlug)
             .HasDatabaseName("IX_CategorySlugHistory_OldSlug");
            // Relationship: cascade-delete when parent category is deleted
            e.HasOne(h => h.Category)
             .WithMany()
             .HasForeignKey(h => h.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ENH-CAT-007 — SEO Canonicalisation overrides
        builder.Entity<SeoMetadata>(e =>
        {
            e.ToTable("SeoMetadata", "catalog");
            e.Property(s => s.EntityType).HasMaxLength(50).IsRequired();
            e.Property(s => s.TitleOverride).HasMaxLength(200);
            e.Property(s => s.MetaDescriptionOverride).HasMaxLength(500);
            e.Property(s => s.CanonicalPathOverride).HasMaxLength(500);
            e.HasIndex(s => new { s.EntityType, s.EntityId }).IsUnique();
        });

        // ENH-PDP-003 — Size Guide Modal
        builder.Entity<SizeGuide>(e =>
        {
            e.ToTable("SizeGuides", "catalog");
            e.Property(g => g.GuideName).HasMaxLength(200).IsRequired();
            e.Property(g => g.ChartJson).HasColumnType("nvarchar(max)").IsRequired();
            e.HasOne(g => g.Brand)
             .WithMany()
             .HasForeignKey(g => g.BrandId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Category)
             .WithMany()
             .HasForeignKey(g => g.CategoryId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
            // One guide per (Brand, Category?) combination
            e.HasIndex(g => new { g.BrandId, g.CategoryId })
             .IsUnique()
             .HasDatabaseName("UX_SizeGuides_Brand_Category");
        });

        // ENH-PDP-006 — Back-in-Stock Subscriptions
        builder.Entity<BackInStockSubscription>(e =>
        {
            e.ToTable("BackInStockSubscriptions", "catalog");
            e.Property(s => s.Email).HasMaxLength(256).IsRequired();
            e.Property(s => s.Phone).HasMaxLength(20);
            e.HasOne(s => s.Product)
             .WithMany()
             .HasForeignKey(s => s.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            // One active subscription per (UserId, ProductId, VariantId)
            e.HasIndex(s => new { s.UserId, s.ProductId, s.VariantId })
             .IsUnique()
             .HasDatabaseName("UX_BackInStockSubscriptions_User_Product_Variant");
            // For batch notifier: un-notified subscriptions for a given product
            e.HasIndex(s => new { s.ProductId, s.NotifiedAt })
             .HasDatabaseName("IX_BackInStockSubscriptions_ProductId_NotifiedAt");
        });

        // ENH-PDP-004 — Q&A Section
        builder.Entity<ProductQuestion>(e =>
        {
            e.ToTable("ProductQuestions", "catalog");
            e.Property(q => q.QuestionText).HasMaxLength(500).IsRequired();
            e.HasOne(q => q.Product)
             .WithMany()
             .HasForeignKey(q => q.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(q => q.ProductId).HasDatabaseName("IX_ProductQuestions_ProductId");
            e.HasIndex(q => q.UserId).HasDatabaseName("IX_ProductQuestions_UserId");
        });

        builder.Entity<ProductAnswer>(e =>
        {
            e.ToTable("ProductAnswers", "catalog");
            e.Property(a => a.AnswerText).HasMaxLength(1000).IsRequired();
            e.Property(a => a.AnswererRole).HasMaxLength(20).IsRequired();
            e.HasOne(a => a.Question)
             .WithMany(q => q.Answers)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.QuestionId).HasDatabaseName("IX_ProductAnswers_QuestionId");
        });

        // Commerce schema
        builder.Entity<Cart>().ToTable("Carts", "commerce");
        builder.Entity<CartItem>().ToTable("CartItems", "commerce");
        builder.Entity<Wishlist>().ToTable("Wishlists", "commerce");
        builder.Entity<WishlistItem>().ToTable("WishlistItems", "commerce");

        // Orders schema — ENH-ORD-001: CK constraints guard valid OrderStatus enum values (0–7)
        builder.Entity<Order>().ToTable("Orders", "orders",
            t => t.HasCheckConstraint("CK_Orders_Status", "[Status] IN (0,1,2,3,4,5,6,7)"));
        builder.Entity<Order>(e =>
        {
            e.Property(o => o.AwbNumber).HasMaxLength(100);
            e.Property(o => o.CarrierName).HasMaxLength(50);
        });
        builder.Entity<OrderItem>().ToTable("OrderItems", "orders");
        builder.Entity<OrderStatusHistory>().ToTable("OrderStatusHistory", "orders",
            t => t.HasCheckConstraint("CK_OrderStatusHistory_Status", "[Status] IN (0,1,2,3,4,5,6,7)"));
        builder.Entity<ShipmentTracking>(e =>
        {
            e.ToTable("ShipmentTrackings", "orders");
            e.Property(t => t.EventType).HasMaxLength(50).IsRequired();
            e.Property(t => t.Description).HasMaxLength(500).IsRequired();
            e.Property(t => t.Location).HasMaxLength(200);
            e.Property(t => t.NdrReason).HasMaxLength(100);
            e.HasOne(t => t.Order)
             .WithMany(o => o.ShipmentTrackings)
             .HasForeignKey(t => t.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.OrderId);
        });

        // Payments schema
        builder.Entity<Payment>().ToTable("Payments", "payments");

        // ENH-PAY-004 — Durable idempotency-key records (composite covering index)
        builder.Entity<IdempotencyKey>(e =>
        {
            e.ToTable("IdempotencyKeys", "payments");
            e.Property(k => k.Endpoint).HasMaxLength(500).IsRequired();
            e.Property(k => k.ResponseBody).HasColumnType("nvarchar(max)").IsRequired();
            // PRIMARY lookup index: each (KeyId, Endpoint) pair is unique
            e.HasIndex(k => new { k.KeyId, k.Endpoint }).IsUnique()
             .HasDatabaseName("IX_IdempotencyKeys_KeyId_Endpoint");
            // ANALYTICAL / ADMIN index: all keys for a given user on a given endpoint
            // INCLUDE covers StatusCode + ExpiresAt so the index is self-sufficient
            e.HasIndex(k => new { k.UserId, k.Endpoint })
             .IncludeProperties(k => new { k.KeyId, k.StatusCode, k.ExpiresAt })
             .HasDatabaseName("IX_IdempotencyKeys_UserId_Endpoint");
            // Expiry-cleanup index: scheduled job DELETE WHERE ExpiresAt < GETUTCDATE()
            e.HasIndex(k => k.ExpiresAt)
             .HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");
        });

        // ENH-PAY-006 — Razorpay vault card tokens (PCI-DSS: no CHD stored)
        builder.Entity<CardToken>(e =>
        {
            e.ToTable("CardTokens", "payments");
            e.Property(t => t.RazorpayTokenId).HasMaxLength(100).IsRequired();
            e.Property(t => t.RazorpayCustomerId).HasMaxLength(100);
            e.Property(t => t.Last4).HasMaxLength(4).IsRequired();
            e.Property(t => t.CardholderName).HasMaxLength(200);
            // One vault token can only be saved once per user
            e.HasIndex(t => new { t.UserId, t.RazorpayTokenId }).IsUnique();
            e.HasIndex(t => t.UserId);
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Admin schema
        builder.Entity<Banner>().ToTable("Banners", "admin");
        builder.Entity<Coupon>(e =>
        {
            e.ToTable("Coupons", "admin");
            // ENH-PROMO-004 — stacking columns
            e.Property(c => c.Category).HasDefaultValue(Infrastructure.Entities.Admin.CouponCategory.Standard);
            e.Property(c => c.AllowsStacking).HasDefaultValue(false);
            e.HasIndex(c => c.AllowsStacking)
             .HasDatabaseName("IX_Coupons_AllowsStacking")
             .HasFilter("[AllowsStacking] = 1");
        });

        // AuditLogs — append-only, no soft-delete filter, no query filter
        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs", "admin");
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(100);
            e.Property(a => a.ActorName).HasMaxLength(256);
            e.Property(a => a.IpAddress).HasMaxLength(50);
            // Indexes for common query patterns
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.HasIndex(a => new { a.ActorId, a.Timestamp });
            e.HasIndex(a => a.ExpiresAt);  // retention cleanup jobs
        });

        // Seller schema
        builder.Entity<Entities.Seller.Seller>().ToTable("Sellers", "seller");
        builder.Entity<SellerInventory>().ToTable("SellerInventory", "seller");
        builder.Entity<SellerPayout>().ToTable("SellerPayouts", "seller");
        // ENH-SELL-002 — KYC document workflow
        builder.Entity<SellerKycDocument>(e =>
        {
            e.ToTable("SellerKycDocuments", "seller");
            e.Property(d => d.DocumentUrl).HasMaxLength(500).IsRequired();
            e.Property(d => d.ReviewNote).HasMaxLength(1000);
            e.HasIndex(d => d.SellerId)
             .HasDatabaseName("IX_SellerKycDocuments_SellerId");
            e.HasIndex(d => d.Status)
             .HasDatabaseName("IX_SellerKycDocuments_Status");
        });

        // Media schema
        builder.Entity<MediaFile>().ToTable("MediaFiles", "media");

        // Wallet schema
        builder.Entity<Wallet>().ToTable("Wallets", "wallet");
        builder.Entity<WalletTransaction>().ToTable("WalletTransactions", "wallet");

        // Analytics schema
        builder.Entity<DailyRevenue>().ToTable("DailyRevenue", "analytics");
        builder.Entity<ProductView>().ToTable("ProductViews", "analytics");
        builder.Entity<SearchTerm>().ToTable("SearchTerms", "analytics");

        // ENH-SRCH-003 — Search Synonyms Dictionary
        builder.Entity<SearchSynonym>(e =>
        {
            e.ToTable("SearchSynonyms", "analytics");
            e.Property(s => s.Term).HasMaxLength(200).IsRequired();
            e.Property(s => s.SynonymsJson).HasColumnType("nvarchar(max)").HasDefaultValue("[]").IsRequired();
            e.HasIndex(s => s.Term)
             .IsUnique()
             .HasDatabaseName("UX_SearchSynonyms_Term");
        });

        // Notifications schema
        builder.Entity<NotificationTemplate>().ToTable("NotificationTemplates", "notifications");
        builder.Entity<NotificationLog>().ToTable("NotificationLogs", "notifications");
        builder.Entity<NotificationOutbox>().ToTable("NotificationOutbox", "notifications");
        builder.Entity<FcmDeviceToken>(e =>
        {
            e.ToTable("FcmDeviceTokens", "notifications");
            e.Property(t => t.DeviceId).HasMaxLength(256).IsRequired();
            e.Property(t => t.Token).HasMaxLength(4096).IsRequired();
            e.Property(t => t.Platform).HasMaxLength(50).IsRequired();
            // Unique constraint: one active token per user per device
            e.HasIndex(t => new { t.UserId, t.DeviceId }).IsUnique();
        });

        // Global soft-delete query filters
        builder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UserAddress>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<OtpCode>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProductVariant>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProductImage>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Review>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AttributeDefinition>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CategoryAttribute>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProductAttribute>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProductVariantOption>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Cart>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CartItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Wishlist>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<WishlistItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<OrderStatusHistory>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ShipmentTracking>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CardToken>().HasQueryFilter(e => !e.IsDeleted); // ENH-PAY-006
        builder.Entity<Banner>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Coupon>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Entities.Seller.Seller>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SellerInventory>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SellerPayout>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<MediaFile>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Wallet>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<WalletTransaction>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<NotificationLog>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<FcmDeviceToken>().HasQueryFilter(e => !e.IsDeleted);
        // ENH-SRCH-003
        builder.Entity<SearchSynonym>().HasQueryFilter(e => !e.IsDeleted);

        // Indexes
        builder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();

        // ENH-CAT-010 — JSON specs column + persisted computed index
        builder.Entity<Product>(e =>
        {
            e.Property(p => p.SpecificationsJson)
             .HasColumnName("SpecificationsJson")
             .HasColumnType("nvarchar(max)");

            // SQL Server persisted computed column: extracts $.material from the JSON blob.
            // 'stored: true' maps to PERSISTED in the DDL, which means the value is
            // physically stored on disk and can be indexed — unlike a non-persisted
            // computed column which must be re-evaluated on every read.
            e.Property(p => p.SpecMaterial)
             .HasComputedColumnSql(
                 "CAST(JSON_VALUE(SpecificationsJson, '$.material') AS nvarchar(200))",
                 stored: true)
             .HasMaxLength(200);

            // Filtered, non-unique index on the persisted column.
            // WHERE SpecMaterial IS NOT NULL avoids index rows for products without a material spec,
            // keeping the index small and selective.
            e.HasIndex(p => p.SpecMaterial)
             .HasFilter("[SpecMaterial] IS NOT NULL")
             .HasDatabaseName("IX_Products_SpecMaterial");
        });
        builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        builder.Entity<Brand>().HasIndex(b => b.Slug).IsUnique();
        builder.Entity<ProductVariant>().HasIndex(v => v.Sku).IsUnique();
        builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        builder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(t => t.Token).IsUnique();
        builder.Entity<Entities.Seller.Seller>().HasIndex(s => s.Slug).IsUnique();
        builder.Entity<Entities.Seller.Seller>().HasIndex(s => s.UserId).IsUnique();
        builder.Entity<Wallet>().HasIndex(w => w.UserId).IsUnique();
        builder.Entity<SearchTerm>().HasIndex(st => st.Term).IsUnique();
        builder.Entity<AttributeDefinition>().HasIndex(a => a.Name).IsUnique();
        builder.Entity<OtpCode>().HasIndex(o => new { o.Email, o.Purpose, o.IsUsed });
        builder.Entity<OtpCode>().HasIndex(o => new { o.PhoneNumber, o.Purpose, o.IsUsed });

        // Self-referencing category hierarchy
        builder.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // CategoryAttribute relationships
        builder.Entity<CategoryAttribute>()
            .HasOne(ca => ca.Category)
            .WithMany(c => c.CategoryAttributes)
            .HasForeignKey(ca => ca.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CategoryAttribute>()
            .HasOne(ca => ca.AttributeDefinition)
            .WithMany(a => a.CategoryAttributes)
            .HasForeignKey(ca => ca.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductAttribute relationships
        builder.Entity<ProductAttribute>()
            .HasOne(pa => pa.Product)
            .WithMany(p => p.Attributes)
            .HasForeignKey(pa => pa.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductAttribute>()
            .HasOne(pa => pa.AttributeDefinition)
            .WithMany(a => a.ProductAttributes)
            .HasForeignKey(pa => pa.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductVariantOption relationships
        builder.Entity<ProductVariantOption>()
            .HasOne(pvo => pvo.ProductVariant)
            .WithMany(v => v.Options)
            .HasForeignKey(pvo => pvo.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductVariantOption>()
            .HasOne(pvo => pvo.AttributeDefinition)
            .WithMany(a => a.VariantOptions)
            .HasForeignKey(pvo => pvo.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductVariantOption>()
            .HasIndex(pvo => new { pvo.ProductVariantId, pvo.AttributeDefinitionId })
            .IsUnique();

        builder.Entity<PincodeServiceability>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CategorySlugHistory>().HasQueryFilter(e => !e.IsDeleted); // ENH-CAT-008
        builder.Entity<FlashSale>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<FlashSaleItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SeoMetadata>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProductQuestion>().HasQueryFilter(e => !e.IsDeleted);             // ENH-PDP-004
        builder.Entity<ProductAnswer>().HasQueryFilter(e => !e.IsDeleted);               // ENH-PDP-004
        builder.Entity<BackInStockSubscription>().HasQueryFilter(e => !e.IsDeleted);     // ENH-PDP-006
        builder.Entity<SizeGuide>().HasQueryFilter(e => !e.IsDeleted);                  // ENH-PDP-003

        // Seller relationships
        builder.Entity<SellerInventory>()
            .HasOne(si => si.Seller)
            .WithMany(s => s.Inventory)
            .HasForeignKey(si => si.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SellerPayout>()
            .HasOne(sp => sp.Seller)
            .WithMany(s => s.Payouts)
            .HasForeignKey(sp => sp.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Wallet relationships
        builder.Entity<WalletTransaction>()
            .HasOne(wt => wt.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
