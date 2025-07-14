using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHypesRatingIndexOnGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Games_Hypes",
                table: "Games",
                column: "Hypes");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Hypes_Rating",
                table: "Games",
                columns: new[] { "Hypes", "Rating" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Hypes",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_Hypes_Rating",
                table: "Games");
        }
    }
}
