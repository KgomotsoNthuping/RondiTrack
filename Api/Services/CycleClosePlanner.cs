using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.DTO;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class CycleClosePlanner
{
    private readonly RondiTrackDbContext _db;

    public CycleClosePlanner(RondiTrackDbContext db)
    {
        _db = db;
    }

    public async Task<CycleClosePreviewResponse> CalculateAsync(
        Stokvel stokvel,
        ContributionCycle cycle,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        if (stokvel.Id != cycle.StokvelId)
        {
            throw new ArgumentException(
                "The cycle belongs to a different stokvel.");
        }

        var members = await _db.StokvelMembers
            .AsNoTracking()
            .Where(member =>
                member.StokvelId == stokvel.Id)
            .OrderBy(member => member.UserId)
            .Select(member => new
            {
                member.UserId,
                member.User.FullName
            })
            .ToListAsync(cancellationToken);

        var contributions = await _db.Contributions
            .AsNoTracking()
            .Where(contribution =>
                contribution.StokvelId == stokvel.Id &&
                contribution.ContributionCycleId == cycle.Id)
            .OrderBy(contribution => contribution.Id)
            .Select(contribution => new
            {
                contribution.Id,
                contribution.UserId,
                contribution.Amount,
                contribution.Status,
                contribution.PaidAtUtc
            })
            .ToListAsync(cancellationToken);

        var collected = contributions
            .Where(contribution =>
                contribution.Status != ContributionStatus.Pending)
            .GroupBy(contribution => contribution.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Amount));

        var requiredCents = ToCents(
            stokvel.MonthlyContribution);

        var arrears = new List<CycleCloseArrearResponse>();

        foreach (var member in members)
        {
            collected.TryGetValue(
                member.UserId,
                out var paid);

            var shortfall = requiredCents - ToCents(paid);

            if (shortfall > 0)
            {
                arrears.Add(
                    new CycleCloseArrearResponse(
                        member.UserId,
                        member.FullName,
                        shortfall));
            }
        }

        var snapshot = new
        {
            stokvel.Id,
            stokvel.MonthlyContribution,
            stokvel.CurrentPeriod,
            stokvel.TotalPeriods,
            CycleId = cycle.Id,
            cycle.CycleNumber,
            cycle.TargetAmount,
            cycle.Status,
            cycle.EndDate,
            cycle.ClosedAtUtc,
            AsOfDate = asOfDate,
            Members = members,
            Contributions = contributions
        };

        var serialized = JsonSerializer.Serialize(snapshot);

        var version = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(serialized)));

        return new CycleClosePreviewResponse(
            stokvel.Id,
            cycle.Id,
            cycle.CycleNumber,
            asOfDate,
            version,
            arrears,
            checked(arrears.Sum(item => item.ShortfallInCents)));
    }

    private static long ToCents(decimal amount)
    {
        return checked(
            (long)decimal.Round(
                amount * 100m,
                0,
                MidpointRounding.AwayFromZero));
    }
}
