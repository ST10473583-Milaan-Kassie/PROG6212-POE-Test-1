using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_AgeRange",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "MinAge",
                table: "Categories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Categories",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 1,
                column: "Distance",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 2,
                column: "Distance",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 3,
                column: "Distance",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 4,
                column: "Distance",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 5,
                column: "Distance",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 6,
                column: "Distance",
                value: null);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "CategoryName", "Distance", "EventID", "MaxAge", "MinAge" },
                values: new object[,]
                {
                    { 7, "Fun Ride", 47.00m, 2, null, null },
                    { 8, "Junior 2.5km", 2.50m, 3, 17, 10 }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_AgeOrDistance",
                table: "Categories",
                sql: "MinAge IS NOT NULL OR Distance IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_AgeRange",
                table: "Categories",
                sql: "MaxAge IS NULL OR (MinAge IS NOT NULL AND MaxAge >= MinAge)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_AgeOrDistance",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_AgeRange",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "MinAge",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_AgeRange",
                table: "Categories",
                sql: "MaxAge IS NULL OR MaxAge >= MinAge");
        }
    }
}
