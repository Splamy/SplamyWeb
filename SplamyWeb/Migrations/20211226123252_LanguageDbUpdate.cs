using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SplamyWeb.Migrations
{
    public partial class LanguageDbUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CommitUrl",
                table: "nightly_project",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "nightly_lang",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "nightly_lang");

            migrationBuilder.AlterColumn<string>(
                name: "CommitUrl",
                table: "nightly_project",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
