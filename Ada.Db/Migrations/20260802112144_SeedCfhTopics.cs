using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ada.Db.Migrations
{
    /// <inheritdoc />
    public partial class SeedCfhTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "moderation_cfh_topics",
                columns: new[] { "id", "category_name", "name", "order" },
                values: new object[,]
                {
                    { 1, "Bullying", "Verbal abuse", 1 },
                    { 2, "Bullying", "Threats", 2 },
                    { 3, "Bullying", "Harassment", 3 },
                    { 4, "Scamming", "Trade scam", 4 },
                    { 5, "Scamming", "Password phishing", 5 },
                    { 6, "Scamming", "Account theft", 6 },
                    { 7, "Inappropriate", "Offensive language", 7 },
                    { 8, "Inappropriate", "Offensive room", 8 },
                    { 9, "Inappropriate", "Offensive name or motto", 9 },
                    { 10, "Other", "Room flooding", 10 },
                    { 11, "Other", "Bot or scripting", 11 },
                    { 12, "Other", "Something else", 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "moderation_cfh_topics",
                keyColumn: "id",
                keyValue: 12);
        }
    }
}
