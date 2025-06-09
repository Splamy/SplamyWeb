using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SplamyWeb.Migrations
{
	public partial class AdjustBlogSchema : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "Summary",
				table: "blog",
				newName: "SummaryHtml");

			migrationBuilder.AlterColumn<string>(
				name: "Value",
				table: "kvp_store",
				type: "text",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "text",
				oldNullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_tabstats_entry_Time",
				table: "tabstats_entry",
				column: "Time");

			migrationBuilder.CreateIndex(
				name: "IX_blog_CreateTime",
				table: "blog",
				column: "CreateTime");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_tabstats_entry_Time",
				table: "tabstats_entry");

			migrationBuilder.DropIndex(
				name: "IX_blog_CreateTime",
				table: "blog");

			migrationBuilder.RenameColumn(
				name: "SummaryHtml",
				table: "blog",
				newName: "Summary");

			migrationBuilder.AlterColumn<string>(
				name: "Value",
				table: "kvp_store",
				type: "text",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "text");
		}
	}
}
