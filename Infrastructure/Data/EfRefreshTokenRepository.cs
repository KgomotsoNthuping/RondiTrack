using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly RondiTrackDbContext _dbContext;

    public EfRefreshTokenRepository(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?>  GetByHashAsync(string tokenHash)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.TokenHash == tokenHash);
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken);

        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveRotationAsync(RefreshToken currentToken,  RefreshToken replacementToken)
    {
        await _dbContext.RefreshTokens.AddAsync(replacementToken);

        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeFamilyAsync(
    Guid familyId,
    DateTime revokedAtUtc)
    {
        await _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                    token => token.RevokedAtUtc, revokedAtUtc));
    }

    public async Task<int>
    DeleteExpiredBeforeAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .RefreshTokens
            .Where(token =>
                token.ExpiresAtUtc < cutoff)
            .ExecuteDeleteAsync(
                cancellationToken);
    }
}