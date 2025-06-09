using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SplamyWeb.Migrations
{
	/// <inheritdoc />
	public partial class SaveJbmInDb : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "Version",
				table: "ramses_song",
				newName: "RamsesVersion");

			migrationBuilder.AddColumn<string>(
				name: "JbmVersion",
				table: "ramses_song",
				type: "text",
				nullable: false,
				defaultValue: "");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "JbmVersion",
				table: "ramses_song");

			migrationBuilder.RenameColumn(
				name: "RamsesVersion",
				table: "ramses_song",
				newName: "Version");
		}
	}
}
