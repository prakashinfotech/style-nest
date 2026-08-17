/**
 * ENH-SRCH-003 — Migration: create analytics.SearchSynonyms table.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Analytics_AddSearchSynonyms : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SearchSynonyms",
            schema: "analytics",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Term         = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                SynonymsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted    = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchSynonyms", x => x.Id);
            });

        // Unique index: one active synonym entry per normalised term.
        // Note: a soft-deleted row keeps the old term slot available for re-creation
        // because the service uses IgnoreQueryFilters + sets IsDeleted=false on upsert.
        migrationBuilder.CreateIndex(
            name: "UX_SearchSynonyms_Term",
            schema: "analytics",
            table: "SearchSynonyms",
            column: "Term",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SearchSynonyms", schema: "analytics");
    }
}
