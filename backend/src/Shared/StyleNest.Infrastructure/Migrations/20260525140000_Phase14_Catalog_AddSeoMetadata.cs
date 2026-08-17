using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddSeoMetadata : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ENH-CAT-007 — SEO Canonicalisation overrides table
        migrationBuilder.CreateTable(
            name: "SeoMetadata",
            schema: "catalog",
            columns: table => new
            {
                Id                      = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EntityType              = table.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                EntityId                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TitleOverride           = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                MetaDescriptionOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CanonicalPathOverride   = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsDeleted               = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt               = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt               = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SeoMetadata", x => x.Id);
            });

        // Unique index enforces one SEO record per (EntityType, EntityId) pair
        migrationBuilder.CreateIndex(
            name: "IX_SeoMetadata_EntityType_EntityId",
            schema: "catalog",
            table: "SeoMetadata",
            columns: ["EntityType", "EntityId"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SeoMetadata", schema: "catalog");
    }
}
