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

    public AuthService(ITrackStore store, IPasswordHasher<User> passwordHasher, IRefreshTokenRepository refreshTokens, TokenService tokenService)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
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

        if (current is null ||
            current.RevokedAtUtc is not null ||
            current.IsExpired(now))
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