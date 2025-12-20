using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderBoardUsingCaching.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexForFasterFetching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Players_Score",
                table: "Players",
                column: "Score");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_Score",
                table: "Players");
        }
    }
}
