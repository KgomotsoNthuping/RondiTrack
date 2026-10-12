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

    public AuthService(ITrackStore store, IPasswordHasher<User> passwordHasher)
    {
        _store = store;
        _passwordHasher = passwordHasher;
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
}