using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <summary>
/// ENH-PROMO-002 — StyleNest Cash Expiry Policy (12-month inactivity).
/// Adds LastPurchaseAt to [wallet].[Wallets] so the expiry service can
/// determine whether a balance has gone stale.
/// </summary>
public partial class Phase14_Wallet_AddLastPurchaseAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Nullable: null means the user has never made a purchase using StyleNest Cash.
        migrationBuilder.AddColumn<DateTime>(
            name: "LastPurchaseAt",
            schema: "wallet",
            table: "Wallets",
            type: "datetime2",
            nullable: true);

        // Index: the expiry batch job queries WHERE LastPurchaseAt < cutoff AND Balance > 0
        migrationBuilder.CreateIndex(
            name: "IX_Wallets_LastPurchaseAt",
            schema: "wallet",
            table: "Wallets",
            column: "LastPurchaseAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Wallets_LastPurchaseAt",
            schema: "wallet",
            table: "Wallets");

        migrationBuilder.DropColumn(
            name: "LastPurchaseAt",
            schema: "wallet",
            table: "Wallets");
    }
}
