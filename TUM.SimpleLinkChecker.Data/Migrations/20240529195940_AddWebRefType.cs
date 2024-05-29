using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TUM.SimpleLinkChecker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebRefType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "WebRefs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "WebRefs");
        }
    }
}
