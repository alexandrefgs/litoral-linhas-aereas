using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MelCdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MelItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CategoryADays = table.Column<int>(type: "int", nullable: true),
                    Restriction = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Procedure = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Verification = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AcarsCheck = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CheckValue = table.Column<int>(type: "int", nullable: true),
                    CheckText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    FuelPenaltyPercent = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    AircraftTypes = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AutoEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MelItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AircraftMelItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MelItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OpenedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AircraftMelItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AircraftMelItems_Aircraft_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "Aircraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AircraftMelItems_MelItems_MelItemId",
                        column: x => x.MelItemId,
                        principalTable: "MelItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlightMelChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PirepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftMelItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightMelChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightMelChecks_AircraftMelItems_AircraftMelItemId",
                        column: x => x.AircraftMelItemId,
                        principalTable: "AircraftMelItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightMelChecks_Pireps_PirepId",
                        column: x => x.PirepId,
                        principalTable: "Pireps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AircraftMelItems_AircraftId_ClosedAt",
                table: "AircraftMelItems",
                columns: new[] { "AircraftId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AircraftMelItems_MelItemId",
                table: "AircraftMelItems",
                column: "MelItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightMelChecks_AircraftMelItemId",
                table: "FlightMelChecks",
                column: "AircraftMelItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightMelChecks_PirepId_AircraftMelItemId",
                table: "FlightMelChecks",
                columns: new[] { "PirepId", "AircraftMelItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MelItems_Code",
                table: "MelItems",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightMelChecks");

            migrationBuilder.DropTable(
                name: "AircraftMelItems");

            migrationBuilder.DropTable(
                name: "MelItems");
        }
    }
}
