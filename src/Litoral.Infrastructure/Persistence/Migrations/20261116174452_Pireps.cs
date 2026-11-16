using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Pireps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Network = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Simulator = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Client = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    AltitudeFt = table.Column<int>(type: "int", nullable: true),
                    GroundSpeedKt = table.Column<int>(type: "int", nullable: true),
                    Heading = table.Column<int>(type: "int", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LastReportAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightSessions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightSessions_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlightTrackPoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AltitudeFt = table.Column<int>(type: "int", nullable: false),
                    AglFt = table.Column<int>(type: "int", nullable: true),
                    IasKt = table.Column<int>(type: "int", nullable: true),
                    GroundSpeedKt = table.Column<int>(type: "int", nullable: false),
                    Heading = table.Column<int>(type: "int", nullable: false),
                    VerticalSpeedFpm = table.Column<int>(type: "int", nullable: true),
                    Flaps = table.Column<int>(type: "int", nullable: true),
                    GearDown = table.Column<bool>(type: "bit", nullable: true),
                    OnGround = table.Column<bool>(type: "bit", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightTrackPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightTrackPoints_FlightSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "FlightSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pireps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PilotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Network = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Callsign = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DepartureIcao = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    PlannedArrivalIcao = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ArrivalIcao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AircraftTypeIcao = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    BlockOffAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlockOnAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TakeoffAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LandingAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlockMinutes = table.Column<int>(type: "int", nullable: false),
                    AirMinutes = table.Column<int>(type: "int", nullable: true),
                    LandingRateFpm = table.Column<int>(type: "int", nullable: true),
                    FuelUsedKg = table.Column<decimal>(type: "decimal(9,1)", precision: 9, scale: 1, nullable: true),
                    FuelAtPushbackKg = table.Column<decimal>(type: "decimal(9,1)", precision: 9, scale: 1, nullable: true),
                    DistanceNm = table.Column<int>(type: "int", nullable: true),
                    FlightRoute = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CruiseAltitudeFt = table.Column<int>(type: "int", nullable: true),
                    OnlinePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScorePoints = table.Column<int>(type: "int", nullable: false),
                    HasRejection = table.Column<bool>(type: "bit", nullable: false),
                    Conformity = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pireps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pireps_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pireps_FlightSessions_FlightSessionId",
                        column: x => x.FlightSessionId,
                        principalTable: "FlightSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pireps_Pilots_PilotId",
                        column: x => x.PilotId,
                        principalTable: "Pilots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PirepEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PirepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PirepEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PirepEvents_Pireps_PirepId",
                        column: x => x.PirepId,
                        principalTable: "Pireps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PirepScoreItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Check = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    PirepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PirepScoreItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PirepScoreItems_Pireps_PirepId",
                        column: x => x.PirepId,
                        principalTable: "Pireps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightSessions_BookingId",
                table: "FlightSessions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSessions_PilotId_Status",
                table: "FlightSessions",
                columns: new[] { "PilotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightSessions_Status",
                table: "FlightSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FlightTrackPoints_SessionId_At",
                table: "FlightTrackPoints",
                columns: new[] { "SessionId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_PirepEvents_PirepId",
                table: "PirepEvents",
                column: "PirepId");

            migrationBuilder.CreateIndex(
                name: "IX_Pireps_BookingId",
                table: "Pireps",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pireps_FlightSessionId",
                table: "Pireps",
                column: "FlightSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pireps_PilotId_SubmittedAt",
                table: "Pireps",
                columns: new[] { "PilotId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pireps_Status_SubmittedAt",
                table: "Pireps",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PirepScoreItems_PirepId",
                table: "PirepScoreItems",
                column: "PirepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightTrackPoints");

            migrationBuilder.DropTable(
                name: "PirepEvents");

            migrationBuilder.DropTable(
                name: "PirepScoreItems");

            migrationBuilder.DropTable(
                name: "Pireps");

            migrationBuilder.DropTable(
                name: "FlightSessions");
        }
    }
}
