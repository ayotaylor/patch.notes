using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIgdbIdIndexToTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_Name",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_Slug",
                table: "Platforms",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Slug",
                table: "Genres",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Franchises_Slug",
                table: "Franchises",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Covers_IgdbId",
                table: "Covers",
                column: "IgdbId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Platforms_Slug",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Genres_Slug",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Franchises_Slug",
                table: "Franchises");

            migrationBuilder.DropIndex(
                name: "IX_Covers_IgdbId",
                table: "Covers");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");
        }
    }
}
