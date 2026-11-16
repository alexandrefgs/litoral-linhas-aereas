using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FleetAndPointsLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AircraftId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PointsCost",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Aircraft",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    AircraftTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentAirportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastFlightAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aircraft", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aircraft_AircraftTypes_AircraftTypeId",
                        column: x => x.AircraftTypeId,
                        principalTable: "AircraftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aircraft_Airports_CurrentAirportId",
                        column: x => x.CurrentAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AircraftId",
                table: "Bookings",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Aircraft_AircraftTypeId",
                table: "Aircraft",
                column: "AircraftTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Aircraft_CurrentAirportId_Status",
                table: "Aircraft",
                columns: new[] { "CurrentAirportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Aircraft_Registration",
                table: "Aircraft",
                column: "Registration",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_PilotId_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "PilotId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Aircraft_AircraftId",
                table: "Bookings",
                column: "AircraftId",
                principalTable: "Aircraft",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Aircraft_AircraftId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Aircraft");

            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AircraftId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "AircraftId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PointsCost",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Bookings");
        }
    }
}
