using Api.DTO;

namespace Api.Services;

public interface IMemberHomeService
{
    Task<MemberHomeResponse> GetAsync(Guid userId);
}