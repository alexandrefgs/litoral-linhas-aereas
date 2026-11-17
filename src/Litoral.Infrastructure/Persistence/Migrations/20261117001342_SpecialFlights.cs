using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpecialFlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpecial",
                table: "Routes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAirportIcao",
                table: "AircraftTypes",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpecialFlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    AircraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinimumRankLevel = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhotoCredit = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialFlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialFlights_Aircraft_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "Aircraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialFlights_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpecialFlightLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialFlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialFlightLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialFlightLegs_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialFlightLegs_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialFlightLegs_SpecialFlights_SpecialFlightId",
                        column: x => x.SpecialFlightId,
                        principalTable: "SpecialFlights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlightLegs_BookingId",
                table: "SpecialFlightLegs",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlightLegs_RouteId",
                table: "SpecialFlightLegs",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlightLegs_SpecialFlightId_Order",
                table: "SpecialFlightLegs",
                columns: new[] { "SpecialFlightId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlights_AircraftId",
                table: "SpecialFlights",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlights_PilotId",
                table: "SpecialFlights",
                column: "PilotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialFlights_Status_EndsAt",
                table: "SpecialFlights",
                columns: new[] { "Status", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialFlightLegs");

            migrationBuilder.DropTable(
                name: "SpecialFlights");

            migrationBuilder.DropColumn(
                name: "IsSpecial",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "DeliveryAirportIcao",
                table: "AircraftTypes");
        }
    }
}
