using Domain.Models;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(
        string tokenHash);

    Task AddAsync(
        RefreshToken refreshToken);

    Task SaveRotationAsync(
        RefreshToken currentToken,
        RefreshToken replacementToken);
}