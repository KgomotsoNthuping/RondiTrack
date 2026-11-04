using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleCloseReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleCloseReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreviewVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrearsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalArrearsInCents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleCloseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleCloseReceipts_ContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CycleCloseReceipts_Users_CallerUserId",
                        column: x => x.CallerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CycleCloseReceipts_Cycle",
                table: "CycleCloseReceipts",
                column: "CycleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CycleCloseReceipts_User_Key",
                table: "CycleCloseReceipts",
                columns: new[] { "CallerUserId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleCloseReceipts");
        }
    }
}
