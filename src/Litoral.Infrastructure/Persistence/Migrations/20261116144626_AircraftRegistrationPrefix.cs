using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AircraftRegistrationPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationPrefix",
                table: "AircraftTypes",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AircraftTypes_RegistrationPrefix",
                table: "AircraftTypes",
                column: "RegistrationPrefix",
                unique: true,
                filter: "[RegistrationPrefix] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AircraftTypes_RegistrationPrefix",
                table: "AircraftTypes");

            migrationBuilder.DropColumn(
                name: "RegistrationPrefix",
                table: "AircraftTypes");
        }
    }
}
