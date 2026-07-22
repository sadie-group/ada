using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ada.Db.Migrations
{
    [DbContext(typeof(AdaMigrationsDbContext))]
    [Migration("20260723000000_AddFurnitureMultiHeights")]
    public partial class AddFurnitureMultiHeights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "multi_heights",
                table: "furniture_items",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "multi_heights",
                table: "furniture_items");
        }
    }
}
