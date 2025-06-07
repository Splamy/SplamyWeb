using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SplamyWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddGinIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ramses_song_Info",
                table: "ramses_song",
                column: "Info")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ramses_song_Info",
                table: "ramses_song");
        }
    }
}
