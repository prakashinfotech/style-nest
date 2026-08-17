using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_Catalog_AddSellerIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                schema: "catalog",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerId",
                schema: "catalog",
                table: "Products");
        }
    }
}
