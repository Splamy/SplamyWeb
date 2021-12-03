using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SplamyWeb.Migrations
{
	public partial class AddBlog : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<DateTime>(
				name: "Time",
				table: "tabstats_entry",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "UploadTime",
				table: "nightly_lang",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "UploadTime",
				table: "nightly_build",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.CreateTable(
				name: "BlogPosts",
				columns: table => new
				{
					PostId = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Visible = table.Column<bool>(type: "boolean", nullable: false),
					CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					Title = table.Column<string>(type: "text", nullable: false),
					Summary = table.Column<string>(type: "text", nullable: false),
					ContentRaw = table.Column<string>(type: "text", nullable: false),
					ContentHtml = table.Column<string>(type: "text", nullable: false),
					Tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_BlogPosts", x => x.PostId);
				});

			migrationBuilder.CreateIndex(
				name: "IX_user_NameNormalized",
				table: "user",
				column: "NameNormalized",
				unique: true);

			migrationBuilder.Sql("CREATE INDEX blog_posts_tags_index ON \"BlogPosts\" USING gin (\"Tags\");");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "BlogPosts");

			migrationBuilder.DropIndex(
				name: "IX_user_NameNormalized",
				table: "user");

			migrationBuilder.AlterColumn<DateTime>(
				name: "Time",
				table: "tabstats_entry",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "UploadTime",
				table: "nightly_lang",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "UploadTime",
				table: "nightly_build",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.Sql("DROP INDEX blog_posts_tags_index IF EXISTS;");
		}
	}
}
