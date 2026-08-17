/**
 * ENH-PDP-003 — Migration: create catalog.SizeGuides table.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddSizeGuides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SizeGuides",
            schema: "catalog",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BrandId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                GuideName  = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ChartJson  = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted  = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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

        // Unique: one guide per (brand, category?) combination
        migrationBuilder.CreateIndex(
            name: "UX_SizeGuides_Brand_Category",
            schema: "catalog",
            table: "SizeGuides",
            columns: ["BrandId", "CategoryId"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SizeGuides", schema: "catalog");
    }
}
