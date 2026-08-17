using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Payments_AddCardToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ENH-PAY-006 — Razorpay vault token table
        // Security note: NO PAN, NO CVV, NO full card number is ever stored here.
        // Only the opaque RazorpayTokenId + display-safe metadata (Last4, Network, Expiry).
        migrationBuilder.CreateTable(
            name: "CardTokens",
            schema: "payments",
            columns: table => new
            {
                Id                  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId              = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RazorpayTokenId     = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RazorpayCustomerId  = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Last4               = table.Column<string>(type: "nvarchar(4)",   maxLength: 4,   nullable: false),
                Network             = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ExpiryMonth         = table.Column<int>(type: "int", nullable: false),
                ExpiryYear          = table.Column<int>(type: "int", nullable: false),
                CardholderName      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsDefault           = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                IsDeleted           = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt           = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt           = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_CardTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // A user cannot save the same Razorpay token twice
        migrationBuilder.CreateIndex(
            name: "IX_CardTokens_UserId_RazorpayTokenId",
            schema: "payments",
            table: "CardTokens",
            columns: ["UserId", "RazorpayTokenId"],
            unique: true);

        // Covering index for GetUserTokensAsync (most frequent query)
        migrationBuilder.CreateIndex(
            name: "IX_CardTokens_UserId",
            schema: "payments",
            table: "CardTokens",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CardTokens", schema: "payments");
    }
}
