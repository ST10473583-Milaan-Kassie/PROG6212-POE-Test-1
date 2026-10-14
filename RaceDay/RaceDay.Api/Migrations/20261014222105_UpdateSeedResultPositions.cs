using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedResultPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Results",
                keyColumn: "ResultID",
                keyValue: 1,
                column: "FinishingPosition",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Results",
                keyColumn: "ResultID",
                keyValue: 2,
                column: "FinishingPosition",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Results",
                keyColumn: "ResultID",
                keyValue: 1,
                column: "FinishingPosition",
                value: 4520);

            migrationBuilder.UpdateData(
                table: "Results",
                keyColumn: "ResultID",
                keyValue: 2,
                column: "FinishingPosition",
                value: 6789);
        }
    }
}
