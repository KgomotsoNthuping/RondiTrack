using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed record ProofDownload(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed class ContributionProofService
{
    private readonly RondiTrackDbContext _db;
    private readonly TimeProvider _clock;

    public ContributionProofService(
        RondiTrackDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ContributionProofResponse> UploadAsync(
        Guid stokvelId,
        Guid contributionId,
        Guid callerUserId,
        Stream input,
        long declaredLength,
        CancellationToken cancellationToken = default)
    {
        if (
            declaredLength <= 0 ||
            declaredLength > ProofFileValidator.MaxFileBytes
        )
        {
            throw InvalidFile(
                "The proof must be a non-empty file of at most 5 MiB.");
        }

        using var output = new MemoryStream();
        var chunk = new byte[64 * 1024];

        while (true)
        {
            var read = await input.ReadAsync(
                chunk.AsMemory(),
                cancellationToken);

            if (read == 0)
            {
                break;
            }

            if (
                output.Length + read >
                ProofFileValidator.MaxFileBytes
            )
            {
                throw InvalidFile(
                    "The proof must not exceed 5 MiB.");
            }

            await output.WriteAsync(
                chunk.AsMemory(0, read),
                cancellationToken);
        }

        if (output.Length != declaredLength)
        {
            throw InvalidFile(
                "The uploaded file length is invalid.");
        }

        var content = output.ToArray();
        var contentType = ProofFileValidator.Validate(
            content);

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var contribution = await FindOwnedContributionAsync(
                stokvelId,
                contributionId,
                callerUserId,
                cancellationToken);

            await LockOpenCycleAsync(
                stokvelId,
                contribution.ContributionCycleId,
                cancellationToken);

            var proof = await _db.ContributionProofs
                .SingleOrDefaultAsync(
                    item =>
                        item.ContributionId == contributionId &&
                        item.StokvelId == stokvelId,
                    cancellationToken);

            var now = _clock.GetUtcNow().UtcDateTime;

            if (proof is null)
            {
                proof = new ContributionProof(
                    stokvelId,
                    contributionId,
                    callerUserId,
                    contentType,
                    content,
                    now);

                _db.ContributionProofs.Add(proof);
            }
            else
            {
                proof.Replace(
                    callerUserId,
                    contentType,
                    content,
                    now);
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return ToResponse(proof);
        });
    }

    public async Task<ContributionProofResponse>
        GetMetadataAsync(
            Guid stokvelId,
            Guid contributionId,
            Guid callerUserId,
            CancellationToken cancellationToken = default)
    {
        await EnsureDownloadAccessAsync(
            stokvelId,
            contributionId,
            callerUserId,
            cancellationToken);

        var proof = await _db.ContributionProofs
            .AsNoTracking()
            .Where(item =>
                item.StokvelId == stokvelId &&
                item.ContributionId == contributionId)
            .Select(item => new ContributionProofResponse(
                item.Id,
                item.StokvelId,
                item.ContributionId,
                item.UploadedByUserId,
                item.ContentType,
                item.SizeBytes,
                item.UploadedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return proof ?? throw MissingProof();
    }

    public async Task<ProofDownload> DownloadAsync(
        Guid stokvelId,
        Guid contributionId,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDownloadAccessAsync(
            stokvelId,
            contributionId,
            callerUserId,
            cancellationToken);

        var proof = await _db.ContributionProofs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.StokvelId == stokvelId &&
                    item.ContributionId == contributionId,
                cancellationToken);

        if (proof is null)
        {
            throw MissingProof();
        }

        var extension = proof.ContentType switch
        {
            "application/pdf" => "pdf",
            "image/png" => "png",
            "image/jpeg" => "jpg",
            _ => throw MissingProof()
        };

        return new ProofDownload(
            proof.Content,
            proof.ContentType,
            $"{proof.Id:N}.{extension}");
    }

    public async Task DeleteAsync(
        Guid stokvelId,
        Guid contributionId,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var contribution = await FindOwnedContributionAsync(
                stokvelId,
                contributionId,
                callerUserId,
                cancellationToken);

            await LockOpenCycleAsync(
                stokvelId,
                contribution.ContributionCycleId,
                cancellationToken);

            var proof = await _db.ContributionProofs
                .SingleOrDefaultAsync(
                    item =>
                        item.StokvelId == stokvelId &&
                        item.ContributionId == contributionId,
                    cancellationToken);

            if (proof is null)
            {
                throw MissingProof();
            }

            _db.ContributionProofs.Remove(proof);

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        });
    }

    private async Task<Contribution>
        FindOwnedContributionAsync(
            Guid stokvelId,
            Guid contributionId,
            Guid callerUserId,
            CancellationToken cancellationToken)
    {
        var membership = await _db.StokvelMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.StokvelId == stokvelId &&
                    member.UserId == callerUserId,
                cancellationToken);

        if (!membership)
        {
            throw MissingProof();
        }

        var contribution = await _db.Contributions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == contributionId &&
                    item.StokvelId == stokvelId &&
                    item.UserId == callerUserId,
                cancellationToken);

        return contribution ?? throw MissingProof();
    }

    private async Task EnsureDownloadAccessAsync(
        Guid stokvelId,
        Guid contributionId,
        Guid callerUserId,
        CancellationToken cancellationToken)
    {
        var contribution = await _db.Contributions
            .AsNoTracking()
            .Where(item =>
                item.StokvelId == stokvelId &&
                item.Id == contributionId)
            .Select(item => new
            {
                item.UserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (contribution is null)
        {
            throw MissingProof();
        }

        var role = await _db.StokvelMembers
            .AsNoTracking()
            .Where(member =>
                member.StokvelId == stokvelId &&
                member.UserId == callerUserId)
            .Select(member =>
                (StokvelMemberRole?)member.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (
            role is null ||
            (
                contribution.UserId != callerUserId &&
                role != StokvelMemberRole.Treasurer
            )
        )
        {
            throw MissingProof();
        }
    }

    private async Task LockOpenCycleAsync(
        Guid stokvelId,
        Guid cycleId,
        CancellationToken cancellationToken)
    {
        var cycles = await _db.ContributionCycles
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM "ContributionCycles"
                WHERE "Id" = {cycleId}
                  AND "StokvelId" = {stokvelId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        var cycle = cycles.SingleOrDefault();

        if (cycle is null)
        {
            throw MissingProof();
        }

        if (
            cycle.ClosedAtUtc.HasValue ||
            cycle.Status == ContributionCycleStatus.PaidOut
        )
        {
            throw new ConflictException(
                "Proof of payment is locked because the cycle is closed.");
        }
    }

    private static ContributionProofResponse ToResponse(
        ContributionProof proof)
    {
        return new ContributionProofResponse(
            proof.Id,
            proof.StokvelId,
            proof.ContributionId,
            proof.UploadedByUserId,
            proof.ContentType,
            proof.SizeBytes,
            proof.UploadedAtUtc);
    }

    private static ResourceNotFoundException MissingProof()
    {
        return new ResourceNotFoundException(
            "Proof of payment was not found.");
    }

    private static ValidationException InvalidFile(
        string message)
    {
        return new ValidationException(
            new Dictionary<string, string[]>
            {
                ["file"] = [message]
            });
    }
}
