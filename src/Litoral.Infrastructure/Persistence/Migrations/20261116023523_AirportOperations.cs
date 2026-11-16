using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    public partial class AirportOperations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Operations",
                table: "Airports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE a SET a.Operations = h.Operations FROM Airports a INNER JOIN Hubs h ON h.AirportId = a.Id");
            migrationBuilder.Sql("UPDATE Airports SET Operations = 1 WHERE Operations = 0");

            migrationBuilder.DropColumn(
                name: "Operations",
                table: "Hubs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Operations",
                table: "Hubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE h SET h.Operations = a.Operations FROM Hubs h INNER JOIN Airports a ON a.Id = h.AirportId");

            migrationBuilder.DropColumn(
                name: "Operations",
                table: "Airports");
        }
    }
}