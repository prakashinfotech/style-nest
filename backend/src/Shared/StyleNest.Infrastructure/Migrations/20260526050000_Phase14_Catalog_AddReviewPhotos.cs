/**
 * ENH-PDP-008 — Migration: add PhotoUrlsJson column to catalog.Reviews.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddReviewPhotos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PhotoUrlsJson",
            schema: "catalog",
            table: "Reviews",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "[]");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PhotoUrlsJson",
            schema: "catalog",
            table: "Reviews");
    }
}
