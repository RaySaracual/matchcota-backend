using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Matchcota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Breed = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dogs_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DogMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DogId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogMedia_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DogAId = table.Column<Guid>(type: "uuid", nullable: false),
                    DogBId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Dogs_DogAId",
                        column: x => x.DogAId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Dogs_DogBId",
                        column: x => x.DogBId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Swipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDogId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDogId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLike = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Swipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Swipes_Dogs_SourceDogId",
                        column: x => x.SourceDogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Swipes_Dogs_TargetDogId",
                        column: x => x.TargetDogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderDogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Dogs_SenderDogId",
                        column: x => x.SenderDogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogMedia_DogId",
                table: "DogMedia",
                column: "DogId");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_Location",
                table: "Dogs",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_OwnerId",
                table: "Dogs",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DogAId_DogBId",
                table: "Matches",
                columns: new[] { "DogAId", "DogBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DogBId",
                table: "Matches",
                column: "DogBId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MatchId",
                table: "Messages",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderDogId",
                table: "Messages",
                column: "SenderDogId");

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_SourceDogId_TargetDogId",
                table: "Swipes",
                columns: new[] { "SourceDogId", "TargetDogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_TargetDogId",
                table: "Swipes",
                column: "TargetDogId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogMedia");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Swipes");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Dogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
