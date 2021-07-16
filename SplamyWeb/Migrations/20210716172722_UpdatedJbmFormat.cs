using Microsoft.EntityFrameworkCore.Migrations;

namespace SplamyWeb.Migrations
{
	public partial class UpdatedJbmFormat : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("TRUNCATE TABLE ramses_map;");
			migrationBuilder.Sql("DELETE FROM ramses_song WHERE \"RawMap\" IS NULL;");
			migrationBuilder.Sql("UPDATE ramses_song SET \"Version\" = 'old';");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
		}
	}
}
