using Microsoft.EntityFrameworkCore.Migrations;

namespace MediaDownloader.Data.Migrations
{
    public partial class AddHistoryPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "History",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Path",
                table: "History");
        }
    }
}
