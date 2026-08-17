using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Seller_AddRlsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ENH-SELL-001 — SQL Server Row-Level Security on seller-owned tables.
            // SESSION_CONTEXT(N'SellerId') is set per-connection by SellerSessionContextInterceptor.
            // When NULL (admin / system), all rows are visible (no restriction).
            // When set to a seller GUID, only rows with matching SellerId are visible.

            // ── SellerInventory predicate ──────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE FUNCTION seller.fn_SellerInventoryAccessPredicate(@SellerId UNIQUEIDENTIFIER)
RETURNS TABLE WITH SCHEMABINDING
AS RETURN
    SELECT 1 AS AccessResult
    WHERE
        SESSION_CONTEXT(N'SellerId') IS NULL OR
        CAST(SESSION_CONTEXT(N'SellerId') AS UNIQUEIDENTIFIER) = @SellerId;
");

            migrationBuilder.Sql(@"
CREATE SECURITY POLICY seller.SellerInventoryRlsPolicy
    ADD FILTER PREDICATE seller.fn_SellerInventoryAccessPredicate(SellerId)
    ON seller.SellerInventory
    WITH (STATE = ON, SCHEMABINDING = ON);
");

            // ── SellerPayouts predicate ────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE FUNCTION seller.fn_SellerPayoutAccessPredicate(@SellerId UNIQUEIDENTIFIER)
RETURNS TABLE WITH SCHEMABINDING
AS RETURN
    SELECT 1 AS AccessResult
    WHERE
        SESSION_CONTEXT(N'SellerId') IS NULL OR
        CAST(SESSION_CONTEXT(N'SellerId') AS UNIQUEIDENTIFIER) = @SellerId;
");

            migrationBuilder.Sql(@"
CREATE SECURITY POLICY seller.SellerPayoutRlsPolicy
    ADD FILTER PREDICATE seller.fn_SellerPayoutAccessPredicate(SellerId)
    ON seller.SellerPayouts
    WITH (STATE = ON, SCHEMABINDING = ON);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS seller.SellerInventoryRlsPolicy;");
            migrationBuilder.Sql("DROP FUNCTION  IF EXISTS seller.fn_SellerInventoryAccessPredicate;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS seller.SellerPayoutRlsPolicy;");
            migrationBuilder.Sql("DROP FUNCTION  IF EXISTS seller.fn_SellerPayoutAccessPredicate;");
        }
    }
}
