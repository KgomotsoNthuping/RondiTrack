namespace Api.DTO;

public class StokvelMemberPageRequest
{
    public int? PageSize { get; init; }

    public string? PageToken { get; init; }

    public string? Sort { get; init; }

    public string? MemberRole { get; init; }
}