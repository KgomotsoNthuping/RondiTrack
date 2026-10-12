using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payouts_ContributionCycleId",
                table: "Payouts");

            migrationBuilder.CreateIndex(
                name: "UX_Payouts_Stokvel_Cycle",
                table: "Payouts",
                columns: new[] { "StokvelId", "ContributionCycleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Payouts_Stokvel_Cycle",
                table: "Payouts");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ContributionCycleId",
                table: "Payouts",
                column: "ContributionCycleId",
                unique: true);
        }
    }
}
