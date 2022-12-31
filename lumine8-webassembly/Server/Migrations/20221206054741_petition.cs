using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lumine8webassembly.Server.Migrations
{
    /// <inheritdoc />
    public partial class petition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StreetName",
                table: "PlacesLived",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PetitionAddresses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LivedId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetitionAddresses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PetitionAddresses_PlacesLived_LivedId",
                        column: x => x.LivedId,
                        principalTable: "PlacesLived",
                        principalColumn: "PlaceLivedId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PetitionAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoLikes",
                columns: table => new
                {
                    VideoLikeId = table.Column<string>(type: "text", nullable: false, defaultValue: "gen_random_uuid()"),
                    VideoId = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Like = table.Column<bool>(type: "boolean", nullable: false),
                    LikeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoLikes", x => x.VideoLikeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryId",
                table: "States",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageOnMessages_MessageId",
                table: "MessageOnMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateId",
                table: "Cities",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_PetitionAddresses_LivedId",
                table: "PetitionAddresses",
                column: "LivedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_States_StateId",
                table: "Cities",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageOnMessages_Messages_MessageId",
                table: "MessageOnMessages",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_States_Countries_CountryId",
                table: "States",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_States_StateId",
                table: "Cities");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageOnMessages_Messages_MessageId",
                table: "MessageOnMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_States_Countries_CountryId",
                table: "States");

            migrationBuilder.DropTable(
                name: "PetitionAddresses");

            migrationBuilder.DropTable(
                name: "VideoLikes");

            migrationBuilder.DropIndex(
                name: "IX_States_CountryId",
                table: "States");

            migrationBuilder.DropIndex(
                name: "IX_MessageOnMessages_MessageId",
                table: "MessageOnMessages");

            migrationBuilder.DropIndex(
                name: "IX_Cities_StateId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "StreetName",
                table: "PlacesLived");
        }
    }
}
