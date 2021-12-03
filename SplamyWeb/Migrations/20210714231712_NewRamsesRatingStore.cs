using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SplamyWeb.Migrations
{
	public partial class NewRamsesRatingStore : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "MaxDifficulty",
				table: "ramses_map");

			migrationBuilder.DropColumn(
				name: "Graph",
				table: "ramses_map");

			migrationBuilder.RenameColumn(
				name: "AvgDifficulty",
				table: "ramses_map",
				newName: "Rating");

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "ramses_song",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

			migrationBuilder.AddColumn<byte[]>(
				name: "RatingDetail",
				table: "ramses_map",
				type: "bytea",
				nullable: false,
				defaultValue: Array.Empty<byte>());

			migrationBuilder.Sql("TRUNCATE TABLE ramses_map;");
			migrationBuilder.Sql("UPDATE ramses_song SET \"Version\" = '1.0-old', \"RawMap\" = NULL;");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "RatingDetail",
				table: "ramses_map");

			migrationBuilder.RenameColumn(
				name: "Rating",
				table: "ramses_map",
				newName: "AvgDifficulty");

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "ramses_song",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

			migrationBuilder.AddColumn<float>(
				name: "MaxDifficulty",
				table: "ramses_map",
				type: "real",
				nullable: false,
				defaultValue: 0f);

			migrationBuilder.AddColumn<float[]>(
				name: "Graph",
				table: "ramses_map",
				type: "real[]",
				nullable: false,
				defaultValue: Array.Empty<float>());
		}
	}
}
