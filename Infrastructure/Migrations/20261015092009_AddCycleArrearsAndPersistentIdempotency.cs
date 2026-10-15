using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleArrearsAndPersistentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arrears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributionCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortfallInCents = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arrears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arrears_ContributionCycles_ContributionCycleId",
                        column: x => x.ContributionCycleId,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Arrears_StokvelMembers_UserId_StokvelId",
                        columns: x => new { x.UserId, x.StokvelId },
                        principalTable: "StokvelMembers",
                        principalColumns: new[] { "UserId", "StokvelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StokvelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributionCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyEntries_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Arrears_ContributionCycleId",
                table: "Arrears",
                column: "ContributionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Arrears_UserId_StokvelId",
                table: "Arrears",
                columns: new[] { "UserId", "StokvelId" });

            migrationBuilder.CreateIndex(
                name: "UX_Arrears_Stokvel_Cycle_User",
                table: "Arrears",
                columns: new[] { "StokvelId", "ContributionCycleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyEntries_ContributionId",
                table: "IdempotencyEntries",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyEntries_CreatedAtUtc",
                table: "IdempotencyEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyEntries_Key",
                table: "IdempotencyEntries",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Arrears");

            migrationBuilder.DropTable(
                name: "IdempotencyEntries");
        }
    }
}
