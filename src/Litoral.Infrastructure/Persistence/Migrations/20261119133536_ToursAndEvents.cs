using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ToursAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TourLegId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AirlineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Airports = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirlineEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinimumRankLevel = table.Column<int>(type: "int", nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    AwardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tours_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AirlineEventFlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PirepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirlineEventFlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AirlineEventFlights_AirlineEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "AirlineEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AirlineEventFlights_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourLegs_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourLegs_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedLegs = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLegAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourProgresses_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourProgresses_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TourLegId",
                table: "Bookings",
                column: "TourLegId");

            migrationBuilder.CreateIndex(
                name: "IX_AirlineEventFlights_EventId_PilotId",
                table: "AirlineEventFlights",
                columns: new[] { "EventId", "PilotId" });

            migrationBuilder.CreateIndex(
                name: "IX_AirlineEventFlights_PilotId",
                table: "AirlineEventFlights",
                column: "PilotId");

            migrationBuilder.CreateIndex(
                name: "IX_AirlineEventFlights_PirepId",
                table: "AirlineEventFlights",
                column: "PirepId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AirlineEvents_IsPublished_StartsAt_EndsAt",
                table: "AirlineEvents",
                columns: new[] { "IsPublished", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourLegs_RouteId",
                table: "TourLegs",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_TourLegs_TourId_Order",
                table: "TourLegs",
                columns: new[] { "TourId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourProgresses_PilotId",
                table: "TourProgresses",
                column: "PilotId");

            migrationBuilder.CreateIndex(
                name: "IX_TourProgresses_TourId_PilotId",
                table: "TourProgresses",
                columns: new[] { "TourId", "PilotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_AwardId",
                table: "Tours",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_IsPublished_EndsAt",
                table: "Tours",
                columns: new[] { "IsPublished", "EndsAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TourLegs_TourLegId",
                table: "Bookings",
                column: "TourLegId",
                principalTable: "TourLegs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TourLegs_TourLegId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "AirlineEventFlights");

            migrationBuilder.DropTable(
                name: "TourLegs");

            migrationBuilder.DropTable(
                name: "TourProgresses");

            migrationBuilder.DropTable(
                name: "AirlineEvents");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TourLegId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TourLegId",
                table: "Bookings");
        }
    }
}
