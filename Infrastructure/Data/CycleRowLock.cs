using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data;

public static class CycleRowLock
{
    public static async Task<bool> LockAsync(
        RondiTrackDbContext db,
        Guid stokvelId,
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "A database transaction is required.");

        await using var command =
            db.Database.GetDbConnection().CreateCommand();

        command.Transaction = transaction.GetDbTransaction();

        command.CommandText =
            """
            SELECT 1
            FROM "ContributionCycles"
            WHERE "Id" = @cycleId
              AND "StokvelId" = @stokvelId
            FOR UPDATE
            """;

        var cycleParameter = command.CreateParameter();
        cycleParameter.ParameterName = "cycleId";
        cycleParameter.Value = cycleId;

        var stokvelParameter = command.CreateParameter();
        stokvelParameter.ParameterName = "stokvelId";
        stokvelParameter.Value = stokvelId;

        command.Parameters.Add(cycleParameter);
        command.Parameters.Add(stokvelParameter);

        return await command.ExecuteScalarAsync(
            cancellationToken) is not null;
    }
}
