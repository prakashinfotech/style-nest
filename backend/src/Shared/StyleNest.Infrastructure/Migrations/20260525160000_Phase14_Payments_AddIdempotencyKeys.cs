using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <summary>
/// ENH-PAY-004 — IdempotencyKeys table with composite covering index.
/// TSD §6.2 / TE-005
///
/// Index strategy:
///   IX_IdempotencyKeys_KeyId_Endpoint   — UNIQUE, primary deduplication lookup
///   IX_IdempotencyKeys_UserId_Endpoint  — analytical, INCLUDE (KeyId, StatusCode, ExpiresAt)
///                                         lets admin queries scan per-user-per-endpoint
///                                         without a clustered-index seek
/// </summary>
public partial class Phase14_Payments_AddIdempotencyKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Ensure the payments schema exists (created by earlier migrations, but guard anyway)
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'payments') " +
                             "EXEC('CREATE SCHEMA [payments]');");

        migrationBuilder.CreateTable(
            name: "IdempotencyKeys",
            schema: "payments",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uniqueidentifier",    nullable: false),
                KeyId        = table.Column<Guid>(type: "uniqueidentifier",    nullable: false),
                UserId       = table.Column<Guid>(type: "uniqueidentifier",    nullable: true),
                Endpoint     = table.Column<string>(type: "nvarchar(500)",     maxLength: 500,  nullable: false),
                StatusCode   = table.Column<int>(type: "int",                  nullable: false),
                ResponseBody = table.Column<string>(type: "nvarchar(max)",     nullable: false),
                ExpiresAt    = table.Column<DateTime>(type: "datetime2",       nullable: false),
                IsDeleted    = table.Column<bool>(type: "bit",                 nullable: false, defaultValue: false),
                CreatedAt    = table.Column<DateTime>(type: "datetime2",       nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt    = table.Column<DateTime>(type: "datetime2",       nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyKeys", x => x.Id);
            });

        // ── PRIMARY deduplication index ──────────────────────────────────────────
        // Unique on (KeyId, Endpoint): the same client UUID cannot be reused for
        // a different endpoint (prevents cross-endpoint replays).
        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyKeys_KeyId_Endpoint",
            schema: "payments",
            table: "IdempotencyKeys",
            columns: ["KeyId", "Endpoint"],
            unique: true);

        // ── ANALYTICAL covering index ─────────────────────────────────────────────
        // Composite on (UserId, Endpoint) INCLUDE (KeyId, StatusCode, ExpiresAt).
        // Allows admin/audit queries to retrieve all idempotency records for a user
        // on a given endpoint without a clustered-index lookup.
        migrationBuilder.Sql("""
            CREATE NONCLUSTERED INDEX [IX_IdempotencyKeys_UserId_Endpoint]
            ON [payments].[IdempotencyKeys] ([UserId], [Endpoint])
            INCLUDE ([KeyId], [StatusCode], [ExpiresAt]);
            """);

        // ── Expiry cleanup index ───────────────────────────────────────────────────
        // Supports a scheduled job that deletes rows WHERE ExpiresAt < GETUTCDATE().
        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyKeys_ExpiresAt",
            schema: "payments",
            table: "IdempotencyKeys",
            column: "ExpiresAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS [IX_IdempotencyKeys_UserId_Endpoint] " +
            "ON [payments].[IdempotencyKeys];");

        migrationBuilder.DropTable(
            name: "IdempotencyKeys",
            schema: "payments");
    }
}
