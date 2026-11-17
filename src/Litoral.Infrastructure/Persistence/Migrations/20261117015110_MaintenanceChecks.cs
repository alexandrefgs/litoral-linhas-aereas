using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaintenanceChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CCheckIntervalHours",
                table: "AircraftTypes",
                type: "int",
                nullable: false,
                defaultValue: 5000);

            migrationBuilder.AddColumn<int>(
                name: "DCheckIntervalHours",
                table: "AircraftTypes",
                type: "int",
                nullable: false,
                defaultValue: 20000);

            migrationBuilder.AddColumn<string>(
                name: "CurrentCheck",
                table: "Aircraft",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinutesSinceCCheck",
                table: "Aircraft",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinutesSinceDCheck",
                table: "Aircraft",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CCheckIntervalHours",
                table: "AircraftTypes");

            migrationBuilder.DropColumn(
                name: "DCheckIntervalHours",
                table: "AircraftTypes");

            migrationBuilder.DropColumn(
                name: "CurrentCheck",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MinutesSinceCCheck",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MinutesSinceDCheck",
                table: "Aircraft");
        }
    }
}
