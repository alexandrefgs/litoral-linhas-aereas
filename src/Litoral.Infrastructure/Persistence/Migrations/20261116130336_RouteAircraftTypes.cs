using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RouteAircraftTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_AircraftTypes_AircraftTypeId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_AircraftTypeId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "AircraftTypeId",
                table: "Routes");

            migrationBuilder.AddColumn<bool>(
                name: "IsCargo",
                table: "Routes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RouteAircraftTypes",
                columns: table => new
                {
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteAircraftTypes", x => new { x.RouteId, x.AircraftTypeId });
                    table.ForeignKey(
                        name: "FK_RouteAircraftTypes_AircraftTypes_AircraftTypeId",
                        column: x => x.AircraftTypeId,
                        principalTable: "AircraftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteAircraftTypes_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteAircraftTypes_AircraftTypeId",
                table: "RouteAircraftTypes",
                column: "AircraftTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteAircraftTypes");

            migrationBuilder.DropColumn(
                name: "IsCargo",
                table: "Routes");

            migrationBuilder.AddColumn<Guid>(
                name: "AircraftTypeId",
                table: "Routes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Routes_AircraftTypeId",
                table: "Routes",
                column: "AircraftTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_AircraftTypes_AircraftTypeId",
                table: "Routes",
                column: "AircraftTypeId",
                principalTable: "AircraftTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
