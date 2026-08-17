using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddFlashSale : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ENH-CAT-002 — Flash Sale Module
        migrationBuilder.CreateTable(
            name: "FlashSales",
            schema: "catalog",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                StartsAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndsAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status    = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FlashSales", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FlashSales_Status_EndsAt",
            schema: "catalog",
            table: "FlashSales",
            columns: ["Status", "EndsAt"]);

        migrationBuilder.CreateTable(
            name: "FlashSaleItems",
            schema: "catalog",
            columns: table => new
            {
                Id            = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FlashSaleId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId     = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SalePrice     = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                StockLimit    = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SoldCount     = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsSoldOut     = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                IsDeleted     = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt     = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt     = table.Column<DateTime>(type: "datetime2", nullable: true),
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

        migrationBuilder.CreateIndex(
            name: "IX_FlashSaleItems_FlashSaleId_ProductId",
            schema: "catalog",
            table: "FlashSaleItems",
            columns: ["FlashSaleId", "ProductId"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FlashSaleItems", schema: "catalog");
        migrationBuilder.DropTable(name: "FlashSales",     schema: "catalog");
    }
}
