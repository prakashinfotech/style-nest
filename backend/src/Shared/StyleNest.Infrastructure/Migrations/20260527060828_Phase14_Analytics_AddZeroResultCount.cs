using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Analytics_AddZeroResultCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPurchaseAt",
                schema: "wallet",
                table: "Wallets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZeroResultCount",
                schema: "analytics",
                table: "SearchTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrlsJson",
                schema: "catalog",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Has360View",
                schema: "catalog",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationsJson",
                schema: "catalog",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwbNumber",
                schema: "orders",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                schema: "orders",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsStacking",
                schema: "admin",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "admin",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpecMaterial",
                schema: "catalog",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                computedColumnSql: "CAST(JSON_VALUE(SpecificationsJson, '$.material') AS nvarchar(200))",
                stored: true);

            migrationBuilder.CreateTable(
                name: "BackInStockSubscriptions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackInStockSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackInStockSubscriptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardTokens",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazorpayTokenId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RazorpayCustomerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Network = table.Column<int>(type: "int", nullable: false),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: false),
                    ExpiryYear = table.Column<int>(type: "int", nullable: false),
                    CardholderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategorySlugHistory",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldSlug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NewSlug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReplacedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySlugHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySlugHistory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FcmDeviceTokens",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcmDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlashSales",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyKeys",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PincodeServiceabilities",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsServiceable = table.Column<bool>(type: "bit", nullable: false),
                    CodEligible = table.Column<bool>(type: "bit", nullable: false),
                    EtaDays = table.Column<int>(type: "int", nullable: false),
                    ExpressAvailable = table.Column<bool>(type: "bit", nullable: false),
                    FreeDeliveryThreshold = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PincodeServiceabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuestions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchSynonyms",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SynonymsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchSynonyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerKycDocuments",
                schema: "seller",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerKycDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerKycDocuments_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalSchema: "seller",
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeoMetadata",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleOverride = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanonicalPathOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentTrackings",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NdrReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentTrackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentTrackings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SizeGuides",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuideName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChartJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeGuides_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "catalog",
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SizeGuides_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FlashSaleItems",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlashSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockLimit = table.Column<int>(type: "int", nullable: false),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    IsSoldOut = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSaleItems_FlashSales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalSchema: "catalog",
                        principalTable: "FlashSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashSaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductAnswers",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswererId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswererRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UpvoteCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAnswers_ProductQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "catalog",
                        principalTable: "ProductQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // SQL Server does not allow a filtered index on a computed column (Error 10609).
            // Use raw DDL without a WHERE clause — still gives O(log n) seeks on SpecMaterial.
            migrationBuilder.Sql(
                "CREATE NONCLUSTERED INDEX [IX_Products_SpecMaterial] ON [catalog].[Products] ([SpecMaterial]);");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_AllowsStacking",
                schema: "admin",
                table: "Coupons",
                column: "AllowsStacking",
                filter: "[AllowsStacking] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BackInStockSubscriptions_ProductId_NotifiedAt",
                schema: "catalog",
                table: "BackInStockSubscriptions",
                columns: new[] { "ProductId", "NotifiedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BackInStockSubscriptions_User_Product_Variant",
                schema: "catalog",
                table: "BackInStockSubscriptions",
                columns: new[] { "UserId", "ProductId", "VariantId" },
                unique: true,
                filter: "[VariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokens_UserId",
                schema: "payments",
                table: "CardTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokens_UserId_RazorpayTokenId",
                schema: "payments",
                table: "CardTokens",
                columns: new[] { "UserId", "RazorpayTokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategorySlugHistory_CategoryId",
                schema: "catalog",
                table: "CategorySlugHistory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySlugHistory_OldSlug",
                schema: "catalog",
                table: "CategorySlugHistory",
                column: "OldSlug");

            migrationBuilder.CreateIndex(
                name: "IX_FcmDeviceTokens_UserId_DeviceId",
                schema: "notifications",
                table: "FcmDeviceTokens",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_FlashSaleId_ProductId",
                schema: "catalog",
                table: "FlashSaleItems",
                columns: new[] { "FlashSaleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_ProductId",
                schema: "catalog",
                table: "FlashSaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_Status_EndsAt",
                schema: "catalog",
                table: "FlashSales",
                columns: new[] { "Status", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_ExpiresAt",
                schema: "payments",
                table: "IdempotencyKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_KeyId_Endpoint",
                schema: "payments",
                table: "IdempotencyKeys",
                columns: new[] { "KeyId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_UserId_Endpoint",
                schema: "payments",
                table: "IdempotencyKeys",
                columns: new[] { "UserId", "Endpoint" })
                .Annotation("SqlServer:Include", new[] { "KeyId", "StatusCode", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PincodeServiceabilities_Pincode",
                schema: "catalog",
                table: "PincodeServiceabilities",
                column: "Pincode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnswers_QuestionId",
                schema: "catalog",
                table: "ProductAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_ProductId",
                schema: "catalog",
                table: "ProductQuestions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_UserId",
                schema: "catalog",
                table: "ProductQuestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_SearchSynonyms_Term",
                schema: "analytics",
                table: "SearchSynonyms",
                column: "Term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerKycDocuments_SellerId",
                schema: "seller",
                table: "SellerKycDocuments",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerKycDocuments_Status",
                schema: "seller",
                table: "SellerKycDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_EntityType_EntityId",
                schema: "catalog",
                table: "SeoMetadata",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackings_OrderId",
                schema: "orders",
                table: "ShipmentTrackings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SizeGuides_CategoryId",
                schema: "catalog",
                table: "SizeGuides",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UX_SizeGuides_Brand_Category",
                schema: "catalog",
                table: "SizeGuides",
                columns: new[] { "BrandId", "CategoryId" },
                unique: true,
                filter: "[CategoryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackInStockSubscriptions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "CardTokens",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "CategorySlugHistory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "FcmDeviceTokens",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "FlashSaleItems",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "IdempotencyKeys",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PincodeServiceabilities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductAnswers",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "SearchSynonyms",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SellerKycDocuments",
                schema: "seller");

            migrationBuilder.DropTable(
                name: "SeoMetadata",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ShipmentTrackings",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "SizeGuides",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "FlashSales",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductQuestions",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Products_SpecMaterial",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_AllowsStacking",
                schema: "admin",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "SpecMaterial",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastPurchaseAt",
                schema: "wallet",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "ZeroResultCount",
                schema: "analytics",
                table: "SearchTerms");

            migrationBuilder.DropColumn(
                name: "PhotoUrlsJson",
                schema: "catalog",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Has360View",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecificationsJson",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AwbNumber",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CarrierName",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AllowsStacking",
                schema: "admin",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "admin",
                table: "Coupons");
        }
    }
}
