using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCoreVideoUpload.Migrations
{
    public partial class RenamingTableToVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UploadVideos",
                table: "UploadVideos");

            migrationBuilder.RenameTable(
                name: "UploadVideos",
                newName: "Videos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Videos",
                table: "Videos",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Videos",
                table: "Videos");

            migrationBuilder.RenameTable(
                name: "Videos",
                newName: "UploadVideos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UploadVideos",
                table: "UploadVideos",
                column: "Id");
        }
    }
}
