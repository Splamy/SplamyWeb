using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

#nullable disable

namespace SplamyWeb.Migrations
{
	/// <inheritdoc />
	public partial class AddInfoColumn : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<JsonDocument>(
				name: "Info",
				table: "ramses_song",
				type: "jsonb",
				defaultValueSql: "jsonb_build_object()",
				nullable: false);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Info",
				table: "ramses_song");
		}
	}
}
