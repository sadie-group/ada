using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Ada.Db.Migrations
{
    [DbContext(typeof(AdaMigrationsDbContext))]
    [Migration("20260723000000_AddPlayerPets")]
    public partial class AddPlayerPets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_pets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    player_id = table.Column<long>(type: "bigint", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<int>(type: "int", nullable: false),
                    race = table.Column<int>(type: "int", nullable: false),
                    color = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    experience = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    energy = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    happiness = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    respect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    has_saddle = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    anyone_can_ride = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    hair_style = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    hair_color = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    publicly_breedable = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    growth_stage = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    rarity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_dead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    x = table.Column<int>(type: "int", nullable: false),
                    y = table.Column<int>(type: "int", nullable: false),
                    z = table.Column<double>(type: "double", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_pets", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_pets_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_player_pets_player_id",
                table: "player_pets",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_pets_room_id",
                table: "player_pets",
                column: "room_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_pets");
        }
    }
}
