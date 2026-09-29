using System.Net;
using System.Net.Http.Json;
using Api.DTO;

namespace Api.Tests.Integration;

public sealed class UserEndpointTests
{
    /// <summary>
    /// A correctly formed user request successfully creates a user and returns 201 Created with the new user's details.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidRequest_Returns201AndCreatedUser()
    {
        // A new factory gives this test its own in-memory application.
        using var factory =
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateUserRequest
            {
                FullName = "Kgomotso B Nthuping",
                Email = "kgomotso@rondi.co.za",
                PhoneNumber = "07433333333"
            };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/users",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);

        Assert.Equal(
            "Kgomotso B Nthuping",
            user.FullName);

        Assert.Equal(
            "kgomotso@rondi.co.za",
            user.Email);

        Assert.NotEqual(
            Guid.Empty,
            user.Id);
    }

    /// <summary>
    /// FluentValidation must reject an invalid user request before normal processing and return 400 Bad Request using the standard problem+json error shape.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithMalformedRequest_Returns400ProblemJson()
    {
        using var factory =
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateUserRequest
            {
                FullName = "",
                Email = "not-an-email",
                PhoneNumber = ""
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/users",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?
                .MediaType);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "\"status\":400",
            body);

        Assert.Contains(
            "correlationId",
            body);
    }
}