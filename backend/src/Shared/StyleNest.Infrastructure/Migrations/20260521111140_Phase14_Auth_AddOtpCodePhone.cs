using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Auth_AddOtpCodePhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "auth",
                table: "OtpCodes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_PhoneNumber_Purpose_IsUsed",
                schema: "auth",
                table: "OtpCodes",
                columns: new[] { "PhoneNumber", "Purpose", "IsUsed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_PhoneNumber_Purpose_IsUsed",
                schema: "auth",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "auth",
                table: "OtpCodes");
        }
    }
}
