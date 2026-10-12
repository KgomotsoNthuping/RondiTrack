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
}