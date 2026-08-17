/**
 * ENH-PDP-004 — Migration: create catalog.ProductQuestions and catalog.ProductAnswers tables.
 */

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Phase14_Catalog_AddProductQnA : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductQuestions",
            schema: "catalog",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId       = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted    = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductQuestions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductQuestions_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProductAnswers",
            schema: "catalog",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuestionId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AnswererId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AnswererRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Shopper"),
                AnswerText   = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                UpvoteCount  = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted    = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductAnswers", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductAnswers_ProductQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalSchema: "catalog",
                    principalTable: "ProductQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductQuestions_ProductId",
            schema: "catalog",
            table: "ProductQuestions",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductQuestions_UserId",
            schema: "catalog",
            table: "ProductQuestions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductAnswers_QuestionId",
            schema: "catalog",
            table: "ProductAnswers",
            column: "QuestionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductAnswers",   schema: "catalog");
        migrationBuilder.DropTable(name: "ProductQuestions", schema: "catalog");
    }
}
