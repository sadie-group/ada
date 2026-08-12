using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "moderation_tickets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    reporter_player_id = table.Column<long>(type: "bigint", nullable: false),
                    reported_player_id = table.Column<long>(type: "bigint", nullable: true),
                    room_id = table.Column<int>(type: "int", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<int>(type: "int", nullable: false),
                    resolution = table.Column<int>(type: "int", nullable: false),
                    picked_by_player_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    picked_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_moderation_tickets_players_picked_by_player_id",
                        column: x => x.picked_by_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_moderation_tickets_players_reported_player_id",
                        column: x => x.reported_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_moderation_tickets_players_reporter_player_id",
                        column: x => x.reporter_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_moderation_tickets_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_tickets_picked_by_player_id",
                table: "moderation_tickets",
                column: "picked_by_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_tickets_reported_player_id",
                table: "moderation_tickets",
                column: "reported_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_tickets_reporter_player_id",
                table: "moderation_tickets",
                column: "reporter_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_tickets_room_id",
                table: "moderation_tickets",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_tickets_state",
                table: "moderation_tickets",
                column: "state");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moderation_tickets");
        }
    }
}
