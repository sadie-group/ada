using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    [DbContext(typeof(AdaMigrationsDbContext))]
    [Migration("20260722000001_AddWiredIntParameters")]
    public partial class AddWiredIntParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "int_parameters",
                table: "player_furniture_item_wired_data",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "int_parameters",
                table: "player_furniture_item_wired_data");
        }
    }
}
