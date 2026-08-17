using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Catalog_AddVariantRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "catalog",
                table: "ProductVariants",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "catalog",
                table: "ProductVariants");
        }
    }
}
