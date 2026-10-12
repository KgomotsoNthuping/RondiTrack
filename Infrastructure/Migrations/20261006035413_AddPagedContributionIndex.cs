using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagedContributionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contributions_StokvelId_UserId_ContributionCycleId",
                table: "Contributions");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions",
                columns: new[] { "StokvelId", "UserId", "ContributionCycleId", "Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_StokvelId_UserId_ContributionCycleId",
                table: "Contributions",
                columns: new[] { "StokvelId", "UserId", "ContributionCycleId" },
                unique: true);
        }
    }
}
