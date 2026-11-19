using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Litoral.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PilotApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PilotApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    HubIcao = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    VatsimId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IvaoId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Simulator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddOns = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Motivation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AcceptedTermsVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PilotApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PilotApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PilotApplications_CreatedAt",
                table: "PilotApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PilotApplications_UserId",
                table: "PilotApplications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationBlocks_EmailHash",
                table: "RegistrationBlocks",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PilotApplications");

            migrationBuilder.DropTable(
                name: "RegistrationBlocks");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
