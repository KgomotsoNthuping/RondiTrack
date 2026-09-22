using Api.Domain;

namespace Api.Data;

//The operations of the application, keeps controllers from storage implementation
public interface ITrackStore
{
    Task<IReadOnlyCollection<User>> GetUsersAsync();

    Task<User?> GetUserByIdAsync(Guid id);

    Task AddUserAsync(User user);

    Task UpdateUserAsync(User user);

    Task<bool> DeleteUserAsync(Guid id);

    Task<bool> IsUserMemberOfAnyStokvelAsync(Guid userId);


    Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync();

    Task<Stokvel?> GetStokvelByIdAsync(Guid id);

    Task AddStokvelAsync(Stokvel stokvel);

    Task UpdateStokvelAsync(Stokvel stokvel);

    Task<bool> DeleteStokvelAsync(Guid id);
}