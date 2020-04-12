using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediaDownloader.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadFolders",
                columns: table => new
                {
                    DownloadFolderId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(nullable: true),
                    LastSelectionDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadFolders", x => x.DownloadFolderId);
                });

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    HistoryRecordId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    DownloadDate = table.Column<DateTime>(nullable: false),
                    DownloadStatus = table.Column<int>(nullable: false),
                    DownloadFormat = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.HistoryRecordId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadFolders");

            migrationBuilder.DropTable(
                name: "History");
        }
    }
}
