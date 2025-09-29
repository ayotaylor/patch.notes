using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalReviewSummaryToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Games_ReviewSummaryId",
                table: "Games",
                column: "ReviewSummaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_ExternalReviewSummaries_ReviewSummaryId",
                table: "Games",
                column: "ReviewSummaryId",
                principalTable: "ExternalReviewSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_ExternalReviewSummaries_ReviewSummaryId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_ReviewSummaryId",
                table: "Games");
        }
    }
}
