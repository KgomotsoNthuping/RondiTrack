using Api.Auth;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public sealed class AuthService
{
    private readonly ITrackStore _store;

    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly IRefreshTokenRepository _refreshTokens;

    private readonly TokenService _tokenService;

    private readonly ILogger<AuthService> _logger;

    public AuthService(ITrackStore store, IPasswordHasher<User> passwordHasher, IRefreshTokenRepository refreshTokens, TokenService tokenService, ILogger<AuthService> logger)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _store.GetUserByEmailAsync(request.Email!);

        if (existingUser is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User(
                request.FullName!,
                request.Email!,
                request.PhoneNumber!);

        var passwordHash = _passwordHasher.HashPassword(
                user,
                request.Password!);

        user.SetPasswordHash(passwordHash);

        await _store.AddUserAsync(user);

        return user;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user =
            await _store.GetUserByEmailAsync(
                request.Email!);

        if (user is null)
        {
            _logger.LogInformation("Sign-in failed: no matching account");
            throw InvalidLogin();
        }

        var verification =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password!);

        if (verification ==
            PasswordVerificationResult.Failed)
        {
            _logger.LogInformation("Sign-in failed for user {UserId}", user.Id);
            throw InvalidLogin();
        }

        var accessToken =
            _tokenService.CreateAccessToken(user);

        var generatedRefresh =
            _tokenService.CreateRefreshToken();

        var refreshToken =
            new RefreshToken(
                user.Id,
                generatedRefresh.Hash,
                Guid.NewGuid(),
                DateTime.UtcNow,
                generatedRefresh.ExpiresAtUtc);

        await _refreshTokens.AddAsync(
            refreshToken);

        _logger.LogInformation("User {UserId} signed in",user.Id);

        return new AuthResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            generatedRefresh.Token,
            generatedRefresh.ExpiresAtUtc);
    }

    public async Task<AuthResult> RefreshAsync(
        string rawRefreshToken)
    {
        var hash =
            _tokenService.HashRefreshToken(
                rawRefreshToken);

        var current =
            await _refreshTokens
                .GetByHashAsync(hash);

        var now =
            DateTime.UtcNow;

        if (current is null)
        {
            throw new AuthenticationFailedException(
                "Refresh token is invalid.");
        }

        if (current.RevokedAtUtc is not null)
        {
            await _refreshTokens
                .RevokeFamilyAsync(
                    current.FamilyId,
                    now);
            _logger.LogWarning("Refresh token reuse detected for user {UserId}", current.UserId);
            throw new AuthenticationFailedException("Refresh token was already used. Please sign in again.");
        }

        if (current.IsExpired(now))
        {
            throw new AuthenticationFailedException(
                "Refresh token is invalid.");
        }

        var user =
            await _store.GetUserByIdAsync(
                current.UserId,
                asNoTracking: true);

        if (user is null)
        {
            throw new AuthenticationFailedException(
                "Refresh token is invalid.");
        }

        var generatedRefresh =
            _tokenService.CreateRefreshToken();

        var replacement =
            new RefreshToken(
                user.Id,
                generatedRefresh.Hash,
                current.FamilyId,
                now,
                generatedRefresh.ExpiresAtUtc);

        current.Rotate(
            replacement.Id,
            now);

        await _refreshTokens
            .SaveRotationAsync(
                current,
                replacement);

        var accessToken =
            _tokenService.CreateAccessToken(user);

        return new AuthResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            generatedRefresh.Token,
            generatedRefresh.ExpiresAtUtc);
    }

    public async Task LogoutAsync(
    string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(
            rawRefreshToken))
        {
            return;
        }

        var hash =
            _tokenService.HashRefreshToken(
                rawRefreshToken);

        var refreshToken =
            await _refreshTokens
                .GetByHashAsync(hash);

        if (refreshToken is null)
        {
            return;
        }

        await _refreshTokens
            .RevokeFamilyAsync(
                refreshToken.FamilyId,
                DateTime.UtcNow);
    }


    private static AuthenticationFailedException
        InvalidLogin()
    {
        return new AuthenticationFailedException(
            "Email or password is incorrect.");
    }
}

public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);