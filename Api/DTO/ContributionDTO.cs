using Domain.Models;

namespace Api.DTO;

    // Client send recording of the payment they make
    public sealed class RecordContributionRequest
    {
        public Guid UserId { get; init; } //maybe use private set

        public Guid ContributionCycleId { get; init; }

        public decimal Amount { get; init; }
    }

    //API sends info back after payment made
    public sealed record ContributionResponse(
    Guid Id,
    Guid StokvelId,
    Guid UserId,
    Guid ContributionCycleId,
    decimal Amount);

    public sealed record CycleContributionResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    StokvelMemberRole MemberRole,
    decimal Amount);
    
    public sealed record ContributionListItemResponse(
        Guid Id,
        long AmountInCents,
        string Status,
        DateTime? PaidAtUtc,
        int CycleNumber);


    public class CycleContributionPageRequest
    {
        public int? PageSize { get; init; }

        public string? PageToken { get; init; }

        public string? Sort { get; init; }

        public Guid? UserId { get; init; }

        public string? MemberRole { get; init; }
    }