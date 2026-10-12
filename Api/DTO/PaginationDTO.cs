namespace Api.DTO;

public class PageRequest
{
    public int? PageSize { get; init; }

    public string? PageToken { get; init; }
}

public record PagedResponse<T>(IReadOnlyCollection<T> Items, string NextPageToken);