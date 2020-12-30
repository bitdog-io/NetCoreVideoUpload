using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCoreVideoUpload.Migrations
{
    public partial class AddedFieldToVideosTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "UploadVideos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "UploadVideos");
        }
    }
}
