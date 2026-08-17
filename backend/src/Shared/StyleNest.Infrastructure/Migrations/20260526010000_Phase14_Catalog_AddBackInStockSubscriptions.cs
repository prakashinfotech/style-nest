/**
 * ENH-PDP-006 — Migration: create catalog.BackInStockSubscriptions table.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddBackInStockSubscriptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BackInStockSubscriptions",
            schema: "catalog",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId     = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VariantId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Email      = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Phone      = table.Column<string>(type: "nvarchar(20)",  maxLength: 20,  nullable: true),
                NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted  = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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

        // Unique: one active subscription per (user, product, variant)
        migrationBuilder.CreateIndex(
            name: "UX_BackInStockSubscriptions_User_Product_Variant",
            schema: "catalog",
            table: "BackInStockSubscriptions",
            columns: ["UserId", "ProductId", "VariantId"],
            unique: true);

        // Supports batch notifier query: WHERE ProductId = ? AND NotifiedAt IS NULL
        migrationBuilder.CreateIndex(
            name: "IX_BackInStockSubscriptions_ProductId_NotifiedAt",
            schema: "catalog",
            table: "BackInStockSubscriptions",
            columns: ["ProductId", "NotifiedAt"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BackInStockSubscriptions", schema: "catalog");
    }
}
