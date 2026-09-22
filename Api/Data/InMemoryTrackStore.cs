using Api.Domain;

namespace Api.Data;

public sealed class InMemoryTrackStore : ITrackStore
{
    private readonly List<User> _users = [];

    private readonly List<Stokvel> _stokvels = [];

    public InMemoryTrackStore()
    {
        SeedData();
    }

    //Test Data of users
    private void SeedData()
    {
        var testOne = new User(
            "Test One",
            "testone@ronditrack.co.za",
            "0711111111");

        var testTwo = new User(
            "Test Two",
            "testtwo@ronditrack.co.za",
            "0722222222");

        var testThree = new User(
            "Kgomotso Nthuping",
            "kgomotso@ronditrack.co.za",
            "0733333333");

        _users.Add(testOne);
        _users.Add(testTwo);
        _users.Add(testThree);

        var ubuntuSavers = new Stokvel(
            "Ubuntu Savers",
            500.00m,
            9,
            12,
            null);

        ubuntuSavers.AddMember(testOne);
        ubuntuSavers.AddMember(testTwo);

        var communitySavers = new Stokvel(
            "Community Builders",
            1000.00m,
            3,
            12,
            "Monthly contributions must be paid before the 5th.");

        communitySavers.AddMember(testThree);

        _stokvels.Add(ubuntuSavers);
        _stokvels.Add(communitySavers);
    }

    //This is readonly to not show the list directly
    public Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        IReadOnlyCollection<User> users = _users.AsReadOnly();

        return Task.FromResult(users);
    }

    public Task<User?> GetUserByIdAsync(Guid id)
    {
        var user =
            _users.FirstOrDefault(user => user.Id == id);

        return Task.FromResult(user);
    }

    public Task AddUserAsync(User user)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user)
    {
        var index =
            _users.FindIndex(existing => existing.Id == user.Id);

        if (index >= 0)
        {
            _users[index] = user;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteUserAsync(Guid id)
    {
        var user =
            _users.FirstOrDefault(user => user.Id == id);

        if (user is null)
        {
            return Task.FromResult(false);
        }

        _users.Remove(user);

        return Task.FromResult(true);
    }


    public Task<bool> IsUserMemberOfAnyStokvelAsync(
        Guid userId)
    {   
        // This checks all stokvels before allowing a user to be deleted
        var isMember =
            _stokvels.Any(stokvel =>
                stokvel.HasMember(userId));

        return Task.FromResult(isMember);
    }

    public Task<IReadOnlyCollection<Stokvel>>
        GetStokvelsAsync()
    {
        IReadOnlyCollection<Stokvel> stokvels =
            _stokvels.AsReadOnly();

        return Task.FromResult(stokvels);
    }

    public Task<Stokvel?> GetStokvelByIdAsync(Guid id)
    {
        var stokvel =
            _stokvels.FirstOrDefault(stokvel =>
                stokvel.Id == id);

        return Task.FromResult(stokvel);
    }

    public Task AddStokvelAsync(Stokvel stokvel)
    {
        _stokvels.Add(stokvel);

        return Task.CompletedTask;
    }

    public Task UpdateStokvelAsync(Stokvel stokvel)
    {
        var index =
            _stokvels.FindIndex(existing =>
                existing.Id == stokvel.Id);

        if (index >= 0)
        {
            _stokvels[index] = stokvel;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteStokvelAsync(Guid id)
    {
        var stokvel =
            _stokvels.FirstOrDefault(stokvel =>
                stokvel.Id == id);

        if (stokvel is null)
        {
            return Task.FromResult(false);
        }

        _stokvels.Remove(stokvel);

        return Task.FromResult(true);
    }
}