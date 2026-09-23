using Api.Contracts;
using Api.Models;

namespace Api.Mapping;

public static class UserMapping
{
    public static User ToDomain(
        this CreateUserRequest request)
    {
        return new User(
            request.FullName,
            request.Email,
            request.PhoneNumber);
    }

    public static void ApplyTo(
        this UpdateUserRequest request,
        User user)
    {
        user.UpdateProfile(
            request.FullName,
            request.Email,
            request.PhoneNumber);
    }

    public static UserResponse ToResponse(
        this User user)
    {
        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber);
    }
}