using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixContributionAndPayoutConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientMemberUserId_RecipientMembe~",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_RecipientMemberUserId_RecipientMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "RecipientMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "RecipientMemberUserId",
                table: "Payouts");

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
                name: "IX_Payouts_RecipientUserId_StokvelId",
                table: "Payouts",
                columns: new[] { "RecipientUserId", "StokvelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StokvelMemberUserId_StokvelMemberStokvelId",
                table: "Payouts",
                columns: new[] { "StokvelMemberUserId", "StokvelMemberStokvelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions",
                columns: new[] { "StokvelId", "ContributionCycleId", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Contributions_Stokvel_User_Cycle",
                table: "Contributions",
                columns: new[] { "StokvelId", "UserId", "ContributionCycleId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientUserId_StokvelId",
                table: "Payouts",
                columns: new[] { "RecipientUserId", "StokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_StokvelMemberUserId_StokvelMemberSto~",
                table: "Payouts",
                columns: new[] { "StokvelMemberUserId", "StokvelMemberStokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientUserId_StokvelId",
                table: "Payouts");

            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_StokvelMemberUserId_StokvelMemberSto~",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_RecipientUserId_StokvelId",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_StokvelMemberUserId_StokvelMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions");

            migrationBuilder.DropIndex(
                name: "UX_Contributions_Stokvel_User_Cycle",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "StokvelMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "StokvelMemberUserId",
                table: "Payouts");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipientMemberStokvelId",
                table: "Payouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RecipientMemberUserId",
                table: "Payouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_RecipientMemberUserId_RecipientMemberStokvelId",
                table: "Payouts",
                columns: new[] { "RecipientMemberUserId", "RecipientMemberStokvelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_PagedCycleQuery",
                table: "Contributions",
                columns: new[] { "StokvelId", "UserId", "ContributionCycleId", "Id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientMemberUserId_RecipientMembe~",
                table: "Payouts",
                columns: new[] { "RecipientMemberUserId", "RecipientMemberStokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
