using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "machine_id",
                table: "banned_machines",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "banned_ip_addresses",
                type: "varchar(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_players_username",
                table: "players",
                column: "username");

            migrationBuilder.CreateIndex(
                name: "ix_player_sso_tokens_token",
                table: "player_sso_tokens",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_banned_machines_machine_id",
                table: "banned_machines",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_banned_ip_addresses_ip_address",
                table: "banned_ip_addresses",
                column: "ip_address");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_players_username",
                table: "players");

            migrationBuilder.DropIndex(
                name: "ix_player_sso_tokens_token",
                table: "player_sso_tokens");

            migrationBuilder.DropIndex(
                name: "ix_banned_machines_machine_id",
                table: "banned_machines");

            migrationBuilder.DropIndex(
                name: "ix_banned_ip_addresses_ip_address",
                table: "banned_ip_addresses");

            migrationBuilder.AlterColumn<string>(
                name: "machine_id",
                table: "banned_machines",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "banned_ip_addresses",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(45)",
                oldMaxLength: 45)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
