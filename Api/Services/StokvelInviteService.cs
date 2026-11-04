using System.Security.Cryptography;
using System.Text;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public sealed class StokvelInviteService
{
    private readonly RondiTrackDbContext _db;
    private readonly TimeProvider _clock;

    public StokvelInviteService(
        RondiTrackDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CreateStokvelInviteResponse>
        CreateAsync(
            Guid stokvelId,
            Guid callerUserId)
    {
        var role = await _db.StokvelMembers
            .AsNoTracking()
            .Where(member =>
                member.StokvelId == stokvelId &&
                member.UserId == callerUserId)
            .Select(member => (StokvelMemberRole?)member.Role)
            .SingleOrDefaultAsync();

        if (role is null)
        {
            throw new ResourceNotFoundException(
                "The stokvel was not found.");
        }

        if (role != StokvelMemberRole.Treasurer)
        {
            throw new InvitePermissionException();
        }

        var code = Convert
            .ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddDays(7);

        var invitation = new StokvelInvite(
            stokvelId,
            callerUserId,
            HashCode(code),
            now,
            expiresAt);

        _db.StokvelInvites.Add(invitation);
        await _db.SaveChangesAsync();

        return new CreateStokvelInviteResponse(
            code,
            expiresAt);
    }

    public async Task<StokvelInvitePreviewResponse>
        PreviewAsync(
            string code,
            Guid callerUserId)
    {
        var invite = await FindActiveAsync(code);

        var stokvel = await _db.Stokvels
            .AsNoTracking()
            .Where(item => item.Id == invite.StokvelId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Rules,
                item.MonthlyContribution,
                item.CurrentPeriod,
                item.TotalPeriods
            })
            .SingleAsync();

        var memberCount = await _db.StokvelMembers
            .AsNoTracking()
            .CountAsync(member =>
                member.StokvelId == invite.StokvelId);

        var alreadyMember = await _db.StokvelMembers
            .AsNoTracking()
            .AnyAsync(member =>
                member.StokvelId == invite.StokvelId &&
                member.UserId == callerUserId);

        var dueDate = await _db.ContributionCycles
            .AsNoTracking()
            .Where(cycle =>
                cycle.StokvelId == invite.StokvelId &&
                cycle.CycleNumber == stokvel.CurrentPeriod)
            .Select(cycle => cycle.EndDate)
            .SingleOrDefaultAsync();

        return new StokvelInvitePreviewResponse(
            stokvel.Id,
            stokvel.Name,
            stokvel.Rules,
            ToCents(stokvel.MonthlyContribution),
            stokvel.CurrentPeriod,
            stokvel.TotalPeriods,
            dueDate,
            memberCount,
            alreadyMember);
    }

    public async Task<JoinStokvelResponse> JoinAsync(
        string code,
        Guid callerUserId)
    {
        var invite = await FindActiveAsync(code);

        var alreadyMember = await _db.StokvelMembers
            .AsNoTracking()
            .AnyAsync(member =>
                member.StokvelId == invite.StokvelId &&
                member.UserId == callerUserId);

        if (alreadyMember)
        {
            throw new ConflictException(
                "You are already a member of this stokvel.");
        }

        var stokvelName = await _db.Stokvels
            .AsNoTracking()
            .Where(stokvel =>
                stokvel.Id == invite.StokvelId)
            .Select(stokvel => stokvel.Name)
            .SingleAsync();

        var membership = new StokvelMember(
            invite.StokvelId,
            callerUserId,
            StokvelMemberRole.Member);

        _db.StokvelMembers.Add(membership);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException error)
            when (
                error.InnerException is PostgresException postgres &&
                postgres.SqlState ==
                    PostgresErrorCodes.UniqueViolation &&
                postgres.ConstraintName ==
                    "PK_StokvelMembers"
            )
        {
            throw new ConflictException(
                "You are already a member of this stokvel.");
        }

        return new JoinStokvelResponse(
            invite.StokvelId,
            stokvelName,
            membership.Role.ToString());
    }

    private async Task<StokvelInvite>
        FindActiveAsync(string code)
    {
        if (
            string.IsNullOrWhiteSpace(code) ||
            code.Length != 43 ||
            code.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character != '-' &&
                character != '_')
        )
        {
            throw new ResourceNotFoundException(
                "The invitation was not found.");
        }

        var hash = HashCode(code);

        var invite = await _db.StokvelInvites
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.CodeHash == hash);

        if (invite is null)
        {
            throw new ResourceNotFoundException(
                "The invitation was not found.");
        }

        if (!invite.IsActive(
            _clock.GetUtcNow().UtcDateTime))
        {
            throw new InviteExpiredException();
        }

        return invite;
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(code));

        return Convert.ToHexString(bytes);
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
