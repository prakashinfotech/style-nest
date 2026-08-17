using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleNest.Infrastructure.Migrations;

/// <summary>
/// ENH-SELL-002 — Seller KYC / Verification Workflow.
/// FR-SELL (TSD §5)
///
/// Creates [seller].[SellerKycDocuments]:
///   Id              uniqueidentifier PK
///   SellerId        FK → seller.Sellers.Id
///   DocumentType    int (enum: GstCertificate=1, PanCard=2, BankStatement=3, AadharCard=4, Other=5)
///   DocumentUrl     nvarchar(500) — URL to the uploaded file in object storage
///   Status          int (enum: Pending=0, UnderReview=1, Approved=2, Rejected=3) DEFAULT 0
///   ReviewedBy      uniqueidentifier NULL — admin userId
///   ReviewedAt      datetime2 NULL
///   ReviewNote      nvarchar(1000) NULL — approval note or rejection reason
///   CreatedAt       datetime2
///   UpdatedAt       datetime2
///   IsDeleted       bit DEFAULT 0
///
/// Indexes:
///   IX_SellerKycDocuments_SellerId  — supports "get all docs for this seller"
///   IX_SellerKycDocuments_Status    — supports admin "get all pending reviews" query
/// </summary>
public partial class Phase14_Seller_AddSellerKycDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name:   "SellerKycDocuments",
            schema: "seller",
            columns: t => new
            {
                Id           = t.Column<Guid>(nullable: false),
                SellerId     = t.Column<Guid>(nullable: false),
                DocumentType = t.Column<int>(nullable: false),
                DocumentUrl  = t.Column<string>(maxLength: 500, nullable: false),
                Status       = t.Column<int>(nullable: false, defaultValue: 0),
                ReviewedBy   = t.Column<Guid>(nullable: true),
                ReviewedAt   = t.Column<DateTime>(nullable: true),
                ReviewNote   = t.Column<string>(maxLength: 1000, nullable: true),
                CreatedAt    = t.Column<DateTime>(nullable: false),
                UpdatedAt    = t.Column<DateTime>(nullable: false),
                IsDeleted    = t.Column<bool>(nullable: false, defaultValue: false),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_SellerKycDocuments", x => x.Id);
                t.ForeignKey(
                    name:              "FK_SellerKycDocuments_Sellers_SellerId",
                    column:            x => x.SellerId,
                    principalSchema:   "seller",
                    principalTable:    "Sellers",
                    principalColumn:   "Id",
                    onDelete:          ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name:    "IX_SellerKycDocuments_SellerId",
            schema:  "seller",
            table:   "SellerKycDocuments",
            column:  "SellerId");

        migrationBuilder.CreateIndex(
            name:   "IX_SellerKycDocuments_Status",
            schema: "seller",
            table:  "SellerKycDocuments",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name:   "SellerKycDocuments",
            schema: "seller");
    }
}
