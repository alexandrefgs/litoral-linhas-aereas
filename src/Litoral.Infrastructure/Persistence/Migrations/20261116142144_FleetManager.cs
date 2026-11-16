using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FleetManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaintenanceIntervalHours",
                table: "AircraftTypes",
                type: "int",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "Cycles",
                table: "Aircraft",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceAt",
                table: "Aircraft",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceReason",
                table: "Aircraft",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MaintenanceStartedAt",
                table: "Aircraft",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MaintenanceUntil",
                table: "Aircraft",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinutesSinceMaintenance",
                table: "Aircraft",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhotoCredit",
                table: "Aircraft",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalMinutes",
                table: "Aircraft",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaintenanceIntervalHours",
                table: "AircraftTypes");

            migrationBuilder.DropColumn(
                name: "Cycles",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceAt",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MaintenanceReason",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MaintenanceStartedAt",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MaintenanceUntil",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "MinutesSinceMaintenance",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "PhotoCredit",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "TotalMinutes",
                table: "Aircraft");
        }
    }
}
