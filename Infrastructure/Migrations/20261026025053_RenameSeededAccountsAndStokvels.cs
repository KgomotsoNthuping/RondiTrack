using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class RenameSeededAccountsAndStokvels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Users"
            SET "FullName" = 'Lerato',
                "Email" = 'lerato@ronditrack.co.za'
            WHERE "Email" = 'lerato@ronditrack.test';

            UPDATE "Users"
            SET "FullName" = 'Nandi',
                "Email" = 'nandi@ronditrack.co.za'
            WHERE "Email" = 'nandi.demo@ronditrack.test';

            UPDATE "Users"
            SET "FullName" = 'Thabo',
                "Email" = 'thabo@ronditrack.co.za'
            WHERE "Email" = 'thabo.demo@ronditrack.test';

            UPDATE "Users"
            SET "FullName" = 'Lerato'
            WHERE "Email" = 'lerato@ronditrack.co.za'
              AND "FullName" = 'Lerato Demo';

            UPDATE "Users"
            SET "FullName" = 'Nandi'
            WHERE "Email" = 'nandi@ronditrack.co.za'
              AND "FullName" = 'Nandi Demo';

            UPDATE "Users"
            SET "FullName" = 'Thabo'
            WHERE "Email" = 'thabo@ronditrack.co.za'
              AND "FullName" = 'Thabo Demo';

            UPDATE "Stokvels"
            SET "Name" = 'Family Savers'
            WHERE "Name" IN (
                'Lerato Family Savers',
                'Lerato Family Savers (Demo)'
            );

            UPDATE "Stokvels"
            SET "Name" = 'Workplace Savers'
            WHERE "Name" IN (
                'Lerato Work Savers',
                'Lerato Work Savers (Demo)',
                'Work Savers'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Automatic reversal of account and stokvel renames is not supported.");
    }
}
