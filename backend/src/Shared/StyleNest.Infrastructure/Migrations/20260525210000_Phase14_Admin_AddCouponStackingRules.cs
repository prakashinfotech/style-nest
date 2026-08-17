/**
 * ENH-PROMO-004 — Migration: add Category (tinyint) and AllowsStacking (bit)
 * columns to admin.Coupons to support configurable coupon stacking rules.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Admin_AddCouponStackingRules : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Category",
            schema: "admin",
            table: "Coupons",
            type: "int",
            nullable: false,
            defaultValue: 0);          // CouponCategory.Standard

        migrationBuilder.AddColumn<bool>(
            name: "AllowsStacking",
            schema: "admin",
            table: "Coupons",
            type: "bit",
            nullable: false,
            defaultValue: false);

        // Partial index — only rows that allow stacking matter for lookup
        migrationBuilder.CreateIndex(
            name: "IX_Coupons_AllowsStacking",
            schema: "admin",
            table: "Coupons",
            column: "AllowsStacking",
            filter: "[AllowsStacking] = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Coupons_AllowsStacking",
            schema: "admin",
            table: "Coupons");

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
