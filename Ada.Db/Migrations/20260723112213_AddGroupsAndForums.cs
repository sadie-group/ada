using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndForums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "admin_only_decoration",
                table: "groups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "badge",
                table: "groups",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "color_a",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "color_b",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "forum_mod_permission",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "forum_post_messages_permission",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "forum_post_threads_permission",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "forum_read_permission",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "has_forum",
                table: "groups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "group_forum_threads",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    group_id = table.Column<int>(type: "int", nullable: false),
                    player_id = table.Column<long>(type: "bigint", nullable: false),
                    subject = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_pinned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    admin_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_forum_threads", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_forum_threads_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_forum_threads_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    group_id = table.Column<int>(type: "int", nullable: false),
                    player_id = table.Column<long>(type: "bigint", nullable: false),
                    rank = table.Column<int>(type: "int", nullable: false),
                    is_pending = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_memberships_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "group_forum_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    thread_id = table.Column<int>(type: "int", nullable: false),
                    player_id = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<int>(type: "int", nullable: false),
                    admin_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_forum_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_forum_messages_group_forum_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "group_forum_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_forum_messages_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_group_forum_messages_player_id",
                table: "group_forum_messages",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_forum_messages_thread_id",
                table: "group_forum_messages",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_forum_threads_group_id",
                table: "group_forum_threads",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_forum_threads_player_id",
                table: "group_forum_threads",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_memberships_group_id_player_id",
                table: "group_memberships",
                columns: new[] { "group_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_memberships_player_id",
                table: "group_memberships",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_forum_messages");

            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "group_forum_threads");

            migrationBuilder.DropColumn(
                name: "admin_only_decoration",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "badge",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "color_a",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "color_b",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "forum_mod_permission",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "forum_post_messages_permission",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "forum_post_threads_permission",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "forum_read_permission",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "has_forum",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "type",
                table: "groups");
        }
    }
}
