using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class EnableXminConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientUserId_StokvelId",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_RecipientUserId_StokvelId",
                table: "Payouts");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Stokvels",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

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

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payouts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ContributionCycles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_RecipientMemberUserId_RecipientMemberStokvelId",
                table: "Payouts",
                columns: new[] { "RecipientMemberUserId", "RecipientMemberStokvelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientMemberUserId_RecipientMembe~",
                table: "Payouts",
                columns: new[] { "RecipientMemberUserId", "RecipientMemberStokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientMemberUserId_RecipientMembe~",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_RecipientMemberUserId_RecipientMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "RecipientMemberStokvelId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "RecipientMemberUserId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ContributionCycles");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_RecipientUserId_StokvelId",
                table: "Payouts",
                columns: new[] { "RecipientUserId", "StokvelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_StokvelMembers_RecipientUserId_StokvelId",
                table: "Payouts",
                columns: new[] { "RecipientUserId", "StokvelId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "UserId", "StokvelId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
