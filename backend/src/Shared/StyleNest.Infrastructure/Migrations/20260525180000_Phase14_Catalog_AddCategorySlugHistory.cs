using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <summary>
/// ENH-CAT-008 — Category Slug 301-Redirect on Rename (EC-CAT-003).
/// Creates [catalog].[CategorySlugHistory] with an index on OldSlug
/// to support O(1) old-slug → current-slug resolution.
/// </summary>
public partial class Phase14_Catalog_AddCategorySlugHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CategorySlugHistory",
            schema: "catalog",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uniqueidentifier",  nullable: false),
                CategoryId = table.Column<Guid>(type: "uniqueidentifier",  nullable: false),
                OldSlug    = table.Column<string>(type: "nvarchar(300)",   maxLength: 300, nullable: false),
                NewSlug    = table.Column<string>(type: "nvarchar(300)",   maxLength: 300, nullable: false),
                ReplacedAt = table.Column<DateTime>(type: "datetime2",     nullable: false),
                IsDeleted  = table.Column<bool>(type: "bit",               nullable: false, defaultValue: false),
                CreatedAt  = table.Column<DateTime>(type: "datetime2",     nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt  = table.Column<DateTime>(type: "datetime2",     nullable: true),
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

        // Primary redirect lookup: resolve old slug to row
        migrationBuilder.CreateIndex(
            name: "IX_CategorySlugHistory_OldSlug",
            schema: "catalog",
            table: "CategorySlugHistory",
            column: "OldSlug");

        // Support "show all historical slugs for a category" queries
        migrationBuilder.CreateIndex(
            name: "IX_CategorySlugHistory_CategoryId",
            schema: "catalog",
            table: "CategorySlugHistory",
            column: "CategoryId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CategorySlugHistory",
            schema: "catalog");
    }
}
