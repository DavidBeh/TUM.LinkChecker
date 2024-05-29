using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TUM.SimpleLinkChecker.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebRefs_Snapshots_TargetId",
                table: "WebRefs");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetId",
                table: "WebRefs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "AnalyzeBecauseReference",
                table: "Snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AnalyzeBecauseUrl",
                table: "Snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_WebRefs_Snapshots_TargetId",
                table: "WebRefs",
                column: "TargetId",
                principalTable: "Snapshots",
                principalColumn: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebRefs_Snapshots_TargetId",
                table: "WebRefs");

            migrationBuilder.DropColumn(
                name: "AnalyzeBecauseReference",
                table: "Snapshots");

            migrationBuilder.DropColumn(
                name: "AnalyzeBecauseUrl",
                table: "Snapshots");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetId",
                table: "WebRefs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WebRefs_Snapshots_TargetId",
                table: "WebRefs",
                column: "TargetId",
                principalTable: "Snapshots",
                principalColumn: "SnapshotId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
