using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerSanctionExpiriesAndAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "mute_expires_at",
                table: "player_data",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trade_lock_expires_at",
                table: "player_data",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "player_achievements",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    player_id = table.Column<long>(type: "bigint", nullable: false),
                    achievement_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    progress = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_achievements", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_player_achievements_player_id_achievement_code",
                table: "player_achievements",
                columns: new[] { "player_id", "achievement_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_achievements");

            migrationBuilder.DropColumn(
                name: "mute_expires_at",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "trade_lock_expires_at",
                table: "player_data");
        }
    }
}
