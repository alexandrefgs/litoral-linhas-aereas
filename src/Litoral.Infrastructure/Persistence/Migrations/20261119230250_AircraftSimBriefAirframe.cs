using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AircraftSimBriefAirframe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SimBriefAirframeId",
                table: "Aircraft",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimBriefAirframeId",
                table: "Aircraft");
        }
    }
}
