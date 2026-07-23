using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshotDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_furniture_item_hand_item_hand_item_hand_items_id",
                table: "furniture_item_hand_item");

            migrationBuilder.DropForeignKey(
                name: "fk_player_role_roles_role_id",
                table: "player_role");

            migrationBuilder.DropForeignKey(
                name: "fk_player_saved_searches_players_player_id",
                table: "player_saved_searches");

            migrationBuilder.DropForeignKey(
                name: "fk_player_tags_players_player_id",
                table: "player_tags");

            migrationBuilder.DropTable(
                name: "player_relationship_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_tags",
                table: "player_tags");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_saved_searches",
                table: "player_saved_searches");

            migrationBuilder.RenameTable(
                name: "player_tags",
                newName: "player_tag");

            migrationBuilder.RenameTable(
                name: "player_saved_searches",
                newName: "player_saved_search");

            migrationBuilder.RenameIndex(
                name: "ix_player_tags_player_id",
                table: "player_tag",
                newName: "ix_player_tag_player_id");

            migrationBuilder.RenameIndex(
                name: "ix_player_saved_searches_player_id",
                table: "player_saved_search",
                newName: "ix_player_saved_search_player_id");

            migrationBuilder.AlterColumn<int>(
                name: "window_y",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 50);

            migrationBuilder.AlterColumn<int>(
                name: "window_x",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 50);

            migrationBuilder.AlterColumn<int>(
                name: "window_width",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 435);

            migrationBuilder.AlterColumn<int>(
                name: "window_height",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 535);

            migrationBuilder.AlterColumn<bool>(
                name: "open_searches",
                table: "player_navigator_settings",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ui_flags",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "trax_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<int>(
                name: "system_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "show_notifications",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "prefer_old_chat",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "furniture_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "block_room_invites",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "block_camera_follow",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                table: "player_bots",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "player_avatar_data",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValue: "M")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "hand_items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_tag",
                table: "player_tag",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_saved_search",
                table: "player_saved_search",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_player_ignores_target_player_id",
                table: "player_ignores",
                column: "target_player_id");

            migrationBuilder.AddForeignKey(
                name: "fk_furniture_item_hand_item_hand_items_hand_items_id",
                table: "furniture_item_hand_item",
                column: "hand_items_id",
                principalTable: "hand_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_ignores_players_target_player_id",
                table: "player_ignores",
                column: "target_player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_role_role_role_id",
                table: "player_role",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_saved_search_players_player_id",
                table: "player_saved_search",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_tag_players_player_id",
                table: "player_tag",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_furniture_item_hand_item_hand_items_hand_items_id",
                table: "furniture_item_hand_item");

            migrationBuilder.DropForeignKey(
                name: "fk_player_ignores_players_target_player_id",
                table: "player_ignores");

            migrationBuilder.DropForeignKey(
                name: "fk_player_role_role_role_id",
                table: "player_role");

            migrationBuilder.DropForeignKey(
                name: "fk_player_saved_search_players_player_id",
                table: "player_saved_search");

            migrationBuilder.DropForeignKey(
                name: "fk_player_tag_players_player_id",
                table: "player_tag");

            migrationBuilder.DropIndex(
                name: "ix_player_ignores_target_player_id",
                table: "player_ignores");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_tag",
                table: "player_tag");

            migrationBuilder.DropPrimaryKey(
                name: "pk_player_saved_search",
                table: "player_saved_search");

            migrationBuilder.RenameTable(
                name: "player_tag",
                newName: "player_tags");

            migrationBuilder.RenameTable(
                name: "player_saved_search",
                newName: "player_saved_searches");

            migrationBuilder.RenameIndex(
                name: "ix_player_tag_player_id",
                table: "player_tags",
                newName: "ix_player_tags_player_id");

            migrationBuilder.RenameIndex(
                name: "ix_player_saved_search_player_id",
                table: "player_saved_searches",
                newName: "ix_player_saved_searches_player_id");

            migrationBuilder.AlterColumn<int>(
                name: "window_y",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "window_x",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "window_width",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                defaultValue: 435,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "window_height",
                table: "player_navigator_settings",
                type: "int",
                nullable: false,
                defaultValue: 535,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "open_searches",
                table: "player_navigator_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<int>(
                name: "ui_flags",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "trax_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "system_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "show_notifications",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "prefer_old_chat",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<int>(
                name: "furniture_volume",
                table: "player_game_settings",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "block_room_invites",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "block_camera_follow",
                table: "player_game_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "player_bots",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "player_avatar_data",
                type: "longtext",
                nullable: false,
                defaultValue: "M",
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "hand_items",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_tags",
                table: "player_tags",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_player_saved_searches",
                table: "player_saved_searches",
                column: "id");

            migrationBuilder.CreateTable(
                name: "player_relationship_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_relationship_types", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "fk_furniture_item_hand_item_hand_item_hand_items_id",
                table: "furniture_item_hand_item",
                column: "hand_items_id",
                principalTable: "hand_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_role_roles_role_id",
                table: "player_role",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_saved_searches_players_player_id",
                table: "player_saved_searches",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_player_tags_players_player_id",
                table: "player_tags",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id");
        }
    }
}
