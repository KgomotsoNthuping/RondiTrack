using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleCloseGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "ContributionCycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "ContributionCycles",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ContributionCycles_Stokvel_CycleNumber",
                table: "ContributionCycles",
                columns: new[] { "StokvelId", "CycleNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ContributionCycles_Stokvel_CycleNumber",
                table: "ContributionCycles");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "ContributionCycles");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ContributionCycles");
        }
    }
}
