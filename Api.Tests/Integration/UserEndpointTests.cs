using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;


namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class UserEndpointTests
{

     private readonly PostgresApiFactory _factory;

    public UserEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }
    /// <summary>
    /// A correctly formed user request successfully creates a user and returns 201 Created with the new user's details.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidRequest_Returns201AndCreatedUser()
    {
        

        using var client = _factory.CreateAuthenticatedClient(
    Guid.NewGuid());

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
                "/api/v1/users",
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
        

        using var client = _factory.CreateAuthenticatedClient(
    Guid.NewGuid());

        var request =
            new CreateUserRequest
            {
                FullName = "",
                Email = "not-an-email",
                PhoneNumber = ""
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/v1/users",
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
