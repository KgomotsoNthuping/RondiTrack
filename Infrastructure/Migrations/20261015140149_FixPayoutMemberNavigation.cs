using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPayoutMemberNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_StokvelMemberUserId_StokvelMemberSto~",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_StokvelMemberUserId_StokvelMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "StokvelMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "StokvelMemberUserId",
                table: "Payouts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StokvelMemberStokvelId",
                table: "Payouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StokvelMemberUserId",
                table: "Payouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StokvelMemberUserId_StokvelMemberStokvelId",
                table: "Payouts",
                columns: new[] { "StokvelMemberUserId", "StokvelMemberStokvelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_StokvelMemberUserId_StokvelMemberSto~",
                table: "Payouts",
                columns: new[] { "StokvelMemberUserId", "StokvelMemberStokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" });
        }
    }
}
