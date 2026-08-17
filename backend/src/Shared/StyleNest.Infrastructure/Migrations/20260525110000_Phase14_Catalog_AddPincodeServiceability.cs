using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Catalog_AddPincodeServiceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PincodeServiceabilities",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsServiceable = table.Column<bool>(type: "bit", nullable: false),
                    CodEligible = table.Column<bool>(type: "bit", nullable: false),
                    EtaDays = table.Column<int>(type: "int", nullable: false),
                    ExpressAvailable = table.Column<bool>(type: "bit", nullable: false),
                    FreeDeliveryThreshold = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 499m),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PincodeServiceabilities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PincodeServiceabilities_Pincode",
                schema: "catalog",
                table: "PincodeServiceabilities",
                column: "Pincode",
                unique: true);

            // ── Seed 12 pincode types ─────────────────────────────────────────
            // Types covered: serviceable/non-serviceable × COD-eligible/blacklisted × express/standard
            var now = new DateTime(2026, 5, 25, 11, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "PincodeServiceabilities",
                columns: new[] { "Id", "Pincode", "IsServiceable", "CodEligible", "EtaDays", "ExpressAvailable", "FreeDeliveryThreshold", "City", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[,]
                {
                    // 1. Mumbai — serviceable, COD eligible, express
                    { Guid.Parse("a1000001-0000-0000-0000-000000000001"), "400001", true,  true,  1, true,  499m, "Mumbai",    now, now, false },
                    // 2. Mumbai — serviceable, COD eligible, standard
                    { Guid.Parse("a1000001-0000-0000-0000-000000000002"), "400002", true,  true,  3, false, 499m, "Mumbai",    now, now, false },
                    // 3. Mumbai — serviceable, COD blacklisted (high fraud), express
                    { Guid.Parse("a1000001-0000-0000-0000-000000000003"), "400003", true,  false, 1, true,  499m, "Mumbai",    now, now, false },
                    // 4. Mumbai — serviceable, COD blacklisted, standard
                    { Guid.Parse("a1000001-0000-0000-0000-000000000004"), "400004", true,  false, 3, false, 499m, "Mumbai",    now, now, false },
                    // 5. Delhi — non-serviceable (remote area)
                    { Guid.Parse("a1000001-0000-0000-0000-000000000005"), "110001", false, false, 0, false, 499m, "Delhi",     now, now, false },
                    // 6. Delhi — non-serviceable
                    { Guid.Parse("a1000001-0000-0000-0000-000000000006"), "110002", false, false, 0, false, 499m, "Delhi",     now, now, false },
                    // 7. Bangalore — serviceable, COD eligible, express
                    { Guid.Parse("a1000001-0000-0000-0000-000000000007"), "560001", true,  true,  2, true,  499m, "Bangalore", now, now, false },
                    // 8. Bangalore — serviceable, COD eligible, standard
                    { Guid.Parse("a1000001-0000-0000-0000-000000000008"), "560002", true,  true,  5, false, 499m, "Bangalore", now, now, false },
                    // 9. Kolkata — serviceable, COD blacklisted, standard
                    { Guid.Parse("a1000001-0000-0000-0000-000000000009"), "700001", true,  false, 4, false, 499m, "Kolkata",   now, now, false },
                    // 10. Hyderabad — serviceable, COD eligible, express
                    { Guid.Parse("a1000001-0000-0000-0000-000000000010"), "500001", true,  true,  1, true,  499m, "Hyderabad", now, now, false },
                    // 11. Chennai — serviceable, COD eligible, standard
                    { Guid.Parse("a1000001-0000-0000-0000-000000000011"), "600001", true,  true,  3, false, 499m, "Chennai",   now, now, false },
                    // 12. Jaipur — non-serviceable (remote)
                    { Guid.Parse("a1000001-0000-0000-0000-000000000012"), "302001", false, false, 0, false, 499m, "Jaipur",    now, now, false },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PincodeServiceabilities",
                schema: "catalog");
        }
    }
}
