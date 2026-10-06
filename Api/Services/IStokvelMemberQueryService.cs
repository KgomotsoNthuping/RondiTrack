using Api.DTO;

namespace Api.Services;

public interface IStokvelMemberQueryService
{
    Task<PagedResponse<UserResponse>>
        GetMembersAsync(
            Guid stokvelId,
            StokvelMemberPageRequest request);
}