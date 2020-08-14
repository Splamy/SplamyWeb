using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SplamyWeb.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ramses_entry",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ramses_entry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ramses_map",
                columns: table => new
                {
                    RamsesId = table.Column<long>(nullable: false),
                    Characteristic = table.Column<string>(nullable: false),
                    Difficulty = table.Column<byte>(nullable: false),
                    MaxDifficulty = table.Column<float>(nullable: false),
                    AvgDifficulty = table.Column<float>(nullable: false),
                    Graph = table.Column<float[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ramses_map", x => new { x.RamsesId, x.Characteristic, x.Difficulty });
                    table.ForeignKey(
                        name: "FK_ramses_map_ramses_entry_RamsesId",
                        column: x => x.RamsesId,
                        principalTable: "ramses_entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ramses_map");

            migrationBuilder.DropTable(
                name: "ramses_entry");
        }
    }
}
