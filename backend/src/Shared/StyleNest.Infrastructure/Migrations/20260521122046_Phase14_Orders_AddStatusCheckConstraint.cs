using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Orders_AddStatusCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatusHistory_Status",
                schema: "orders",
                table: "OrderStatusHistory",
                sql: "[Status] IN (0,1,2,3,4,5,6,7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                schema: "orders",
                table: "Orders",
                sql: "[Status] IN (0,1,2,3,4,5,6,7)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatusHistory_Status",
                schema: "orders",
                table: "OrderStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                schema: "orders",
                table: "Orders");
        }
    }
}
