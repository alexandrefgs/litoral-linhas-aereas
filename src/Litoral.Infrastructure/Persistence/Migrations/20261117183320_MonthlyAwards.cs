using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MonthlyAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PilotNumber = table.Column<int>(type: "int", nullable: false),
                    PilotName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyAwards_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyAwards_PilotId",
                table: "MonthlyAwards",
                column: "PilotId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyAwards_Year_Month_Category_Position",
                table: "MonthlyAwards",
                columns: new[] { "Year", "Month", "Category", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyAwards");
        }
    }
}
