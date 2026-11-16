using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    public partial class PilotBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseIcao",
                table: "Pilots",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Pilots SET BaseIcao = CurrentLocationIcao");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseIcao",
                table: "Pilots");
        }
    }
}