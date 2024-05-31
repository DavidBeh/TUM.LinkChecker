using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TUM.SimpleLinkChecker.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scrapes",
                columns: table => new
                {
                    ScrapeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExceptionType = table.Column<string>(type: "TEXT", nullable: true),
                    Finished = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scrapes", x => x.ScrapeId);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Uri = table.Column<string>(type: "TEXT", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AnalyzeBecauseUrl = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnalyzeBecauseReference = table.Column<bool>(type: "INTEGER", nullable: false),
                    Downloaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExceptionType = table.Column<string>(type: "TEXT", nullable: true),
                    ContentScraped = table.Column<bool>(type: "INTEGER", nullable: true),
                    ContentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ContentExceptionType = table.Column<string>(type: "TEXT", nullable: true),
                    C_Title = table.Column<string>(type: "TEXT", nullable: true),
                    C_Description = table.Column<string>(type: "TEXT", nullable: true),
                    C_Typo3PageId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.SnapshotId);
                    table.ForeignKey(
                        name: "FK_Snapshots_Scrapes_ScrapeId",
                        column: x => x.ScrapeId,
                        principalTable: "Scrapes",
                        principalColumn: "ScrapeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebRefs",
                columns: table => new
                {
                    WebRefId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LinkText = table.Column<string>(type: "TEXT", nullable: true),
                    RawLink = table.Column<string>(type: "TEXT", nullable: true),
                    XPath = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    LinkMalformed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebRefs", x => x.WebRefId);
                    table.ForeignKey(
                        name: "FK_WebRefs_Snapshots_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Snapshots",
                        principalColumn: "SnapshotId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebRefs_Snapshots_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Snapshots",
                        principalColumn: "SnapshotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_ScrapeId",
                table: "Snapshots",
                column: "ScrapeId");

            migrationBuilder.CreateIndex(
                name: "IX_WebRefs_SourceId",
                table: "WebRefs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WebRefs_TargetId",
                table: "WebRefs",
                column: "TargetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebRefs");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropTable(
                name: "Scrapes");
        }
    }
}
