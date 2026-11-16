using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCrewAssignedAt",
                table: "Pilots",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CrewPilotId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "MonthlyRosters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    FlightsPerDay = table.Column<int>(type: "int", nullable: false),
                    BaseIcao = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyRosters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyRosters_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterLegs_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterLegs_MonthlyRosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "MonthlyRosters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterLegs_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CrewPilotId_Status",
                table: "Bookings",
                columns: new[] { "CrewPilotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                table: "Bookings",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyRosters_PilotId_Year_Month",
                table: "MonthlyRosters",
                columns: new[] { "PilotId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RosterLegs_BookingId",
                table: "RosterLegs",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterLegs_RosterId_Order",
                table: "RosterLegs",
                columns: new[] { "RosterId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RosterLegs_RouteId",
                table: "RosterLegs",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Pilots_CrewPilotId",
                table: "Bookings",
                column: "CrewPilotId",
                principalTable: "Pilots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Pilots_CrewPilotId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "RosterLegs");

            migrationBuilder.DropTable(
                name: "MonthlyRosters");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CrewPilotId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "LastCrewAssignedAt",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CrewPilotId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Bookings");
        }
    }
}
