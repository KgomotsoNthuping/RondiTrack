using Api.Data;
using Api.Models;

namespace Api.Services;

public sealed class TrackService
    : ITrackService
{
    private readonly ITrackStore _store;
    private readonly IIdempotency _idempotencyStore;

    // Keeps the contribution check and write together
    // while using the in-memory implementation.
    private readonly SemaphoreSlim _contributionLock =
        new(1, 1);

    public TrackService(
        ITrackStore store,
        IIdempotency idempotencyStore)
    {
        _store = store;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<ServiceResult<User>>
        AddMemberAsync(
            Guid stokvelId,
            Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return ServiceResult<User>.NotFound(
                "The stokvel was not found.");
        }

        var user =
            await _store.GetUserByIdAsync(userId);

        if (user is null)
        {
            return ServiceResult<User>.NotFound(
                "The user was not found.");
        }

        try
        {
            // The entity remains the final guard against
            // duplicate membership.
            stokvel.AddMember(user);
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult<User>.Conflict(
                exception.Message);
        }

        await _store.UpdateStokvelAsync(stokvel);

        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult<bool>>
        RemoveMemberAsync(
            Guid stokvelId,
            Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return ServiceResult<bool>.NotFound(
                "The stokvel was not found.");
        }

        var removed =
            stokvel.RemoveMember(userId);

        if (!removed)
        {
            return ServiceResult<bool>.NotFound(
                "The user is not a member of this stokvel.");
        }

        await _store.UpdateStokvelAsync(stokvel);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>>
        DeleteUserAsync(Guid userId)
    {
        var user =
            await _store.GetUserByIdAsync(userId);

        if (user is null)
        {
            return ServiceResult<bool>.NotFound(
                "The user was not found.");
        }

        // This decision used to be inside UsersController.
        var belongsToStokvel =
            await _store
                .IsUserMemberOfAnyStokvelAsync(userId);

        if (belongsToStokvel)
        {
            return ServiceResult<bool>.Conflict(
                "The user cannot be deleted while they are still a member of a stokvel.");
        }

        await _store.DeleteUserAsync(userId);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<Contribution>>
        RecordContributionAsync(
            Guid stokvelId,
            Guid userId,
            int cycleNumber,
            decimal amount,
            string? idempotencyKey)
    {
        // Missing Idempotency-Key means the HTTP request
        // does not satisfy the contract for money-moving operations.
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ServiceResult<Contribution>.BadRequest(
                "An Idempotency-Key header is required.");
        }

        var signature =
            new ContributionRequestSignature(
                stokvelId,
                userId,
                cycleNumber,
                amount);

        await _contributionLock.WaitAsync();

        try
        {
            // Check idempotency before attempting another write.
            var previous =
                await _idempotencyStore
                    .GetAsync(idempotencyKey);

            if (previous is not null)
            {
                // Same key + different request must be rejected.
                if (previous.Signature != signature)
                {
                    return ServiceResult<Contribution>.Conflict(
                        "This Idempotency-Key has already been used with a different request.");
                }

                // Same key + same request returns the original result.
                return ServiceResult<Contribution>.Success(
                    previous.Contribution);
            }

            if (userId == Guid.Empty)
            {
                return ServiceResult<Contribution>.Unprocessable(
                    "A valid user ID is required.");
            }

            var stokvel =
                await _store.GetStokvelByIdAsync(stokvelId);

            if (stokvel is null)
            {
                return ServiceResult<Contribution>.NotFound(
                    "The stokvel was not found.");
            }

            var user =
                await _store.GetUserByIdAsync(userId);

            if (user is null)
            {
                return ServiceResult<Contribution>.NotFound(
                    "The user was not found.");
            }

            // A User may only contribute to a Stokvel
            // that they actually belong to.
            if (!stokvel.HasMember(userId))
            {
                return ServiceResult<Contribution>.Unprocessable(
                    "The user is not a member of this stokvel.");
            }

            if (cycleNumber < 1 ||
                cycleNumber > stokvel.TotalPeriods)
            {
                return ServiceResult<Contribution>.Unprocessable(
                    $"The cycle number must be between 1 and {stokvel.TotalPeriods}.");
            }

            if (amount <= 0)
            {
                return ServiceResult<Contribution>.Unprocessable(
                    "The contribution amount must be greater than zero.");
            }

            // For this version of RondiTrack, a member must pay
            // exactly the Stokvel's required contribution.
            if (amount != stokvel.MonthlyContribution)
            {
                return ServiceResult<Contribution>.Unprocessable(
                    $"The required contribution is {stokvel.MonthlyContribution:C}.");
            }

            var existingContribution =
                await _store.GetContributionAsync(
                    stokvelId,
                    userId,
                    cycleNumber);

            // Even a new idempotency key cannot record
            // the same member/cycle payment twice.
            if (existingContribution is not null)
            {
                return ServiceResult<Contribution>.Conflict(
                    "This member's contribution has already been recorded for this cycle.");
            }

            var contribution =
                new Contribution(
                    stokvelId,
                    userId,
                    cycleNumber,
                    amount);

            await _store.AddContributionAsync(
                contribution);

            // Store the successful result against the key.
            await _idempotencyStore.SaveAsync(
                idempotencyKey,
                new IdempotencyRecord(
                    signature,
                    contribution));

            return ServiceResult<Contribution>.Success(
                contribution);
        }
        finally
        {
            _contributionLock.Release();
        }
    }
}