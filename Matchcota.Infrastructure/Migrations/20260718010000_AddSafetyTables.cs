using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchcota.Infrastructure.Migrations
{
    public partial class AddSafetyTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedDogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blocks_Dogs_BlockedDogId",
                        column: x => x.BlockedDogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blocks_Users_BlockerUserId",
                        column: x => x.BlockerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedDogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyReports_Dogs_ReportedDogId",
                        column: x => x.ReportedDogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SafetyReports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BlockedDogId",
                table: "Blocks",
                column: "BlockedDogId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BlockerUserId_BlockedDogId",
                table: "Blocks",
                columns: new[] { "BlockerUserId", "BlockedDogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyReports_ReportedByUserId",
                table: "SafetyReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyReports_ReportedDogId",
                table: "SafetyReports",
                column: "ReportedDogId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Blocks");
            migrationBuilder.DropTable(name: "SafetyReports");
        }
    }
}
