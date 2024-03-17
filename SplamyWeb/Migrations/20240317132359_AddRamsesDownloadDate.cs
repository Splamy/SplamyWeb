using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SplamyWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRamsesDownloadDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DownloadDate",
                table: "ramses_song",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadDate",
                table: "ramses_song");
        }
    }
}
