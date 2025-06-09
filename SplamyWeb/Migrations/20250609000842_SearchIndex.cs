using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace SplamyWeb.Migrations
{
    /// <inheritdoc />
    public partial class SearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchIndexVersion",
                table: "ramses_map",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "ramses_map",
                type: "tsvector",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ramses_map_SearchVector",
                table: "ramses_map",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ramses_map_SearchVector",
                table: "ramses_map");

            migrationBuilder.DropColumn(
                name: "SearchIndexVersion",
                table: "ramses_map");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "ramses_map");
        }
    }
}
