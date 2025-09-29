using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExternalReviewSummaryInGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_ExternalReviewSummaries_ReviewSummaryId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_ReviewSummaryId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "ExternalReviewSummaries");

            migrationBuilder.DropColumn(
                name: "ReviewSummaryId",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "ReviewSummary",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExternalReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExternalReviewerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GameId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Review = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp", nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalReviews_ExternalReviewers_ExternalReviewerId",
                        column: x => x.ExternalReviewerId,
                        principalTable: "ExternalReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalReviews_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalReviews_ExternalReviewerId_GameId",
                table: "ExternalReviews",
                columns: new[] { "ExternalReviewerId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalReviews_GameId",
                table: "ExternalReviews",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalReviews");

            migrationBuilder.DropColumn(
                name: "ReviewSummary",
                table: "Games");

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewSummaryId",
                table: "Games",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ExternalReviewSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExternalReviewerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GameId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Summary = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp", nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalReviewSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalReviewSummaries_ExternalReviewers_ExternalReviewerId",
                        column: x => x.ExternalReviewerId,
                        principalTable: "ExternalReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalReviewSummaries_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Games_ReviewSummaryId",
                table: "Games",
                column: "ReviewSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalReviewSummaries_ExternalReviewerId_GameId",
                table: "ExternalReviewSummaries",
                columns: new[] { "ExternalReviewerId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalReviewSummaries_GameId",
                table: "ExternalReviewSummaries",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_ExternalReviewSummaries_ReviewSummaryId",
                table: "Games",
                column: "ReviewSummaryId",
                principalTable: "ExternalReviewSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
