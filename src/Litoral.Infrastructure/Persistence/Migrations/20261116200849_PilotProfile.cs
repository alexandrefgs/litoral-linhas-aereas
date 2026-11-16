using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PilotProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Pilots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Pilots",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Pilots",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "Pilots",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordId",
                table: "Pilots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IvaoId",
                table: "Pilots",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Pilots",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimAircraftTypes",
                table: "Pilots",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Pilots",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatsimId",
                table: "Pilots",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pilots_IvaoId",
                table: "Pilots",
                column: "IvaoId",
                unique: true,
                filter: "[IvaoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pilots_VatsimId",
                table: "Pilots",
                column: "VatsimId",
                unique: true,
                filter: "[VatsimId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pilots_IvaoId",
                table: "Pilots");

            migrationBuilder.DropIndex(
                name: "IX_Pilots_VatsimId",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "IvaoId",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "SimAircraftTypes",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Pilots");

            migrationBuilder.DropColumn(
                name: "VatsimId",
                table: "Pilots");
        }
    }
}
