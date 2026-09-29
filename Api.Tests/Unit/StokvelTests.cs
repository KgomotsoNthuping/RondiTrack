using Api.Models;

namespace Api.Tests.Unit;

/// <summary>
/// Testing that the same user cannot appear in the same stokvel twice
/// </summary>
public sealed class StokvelTests
{
    [Fact]
    public void AddMember_WhenUserAlreadyBelongsToStokvel_ThrowsException()
    {
        // Arrange
        var user = new User(
            "Kgomotso Nthuping",
            "kgomotso@rondi.co.za",
            "0711111111");

        var stokvel = new Stokvel(
            "Ubuntu Savers",
            500m,
            1,
            12);

        stokvel.AddMember(user);

        // Act
        var action = () =>
            stokvel.AddMember(user);

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);

        Assert.Single(stokvel.Members);
    }
}