using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivityTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Activities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Activities");
        }
    }
}
