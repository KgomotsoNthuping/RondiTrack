    using Api.Data;
    using Api.Models;

    namespace Api.Services;

    public sealed class TrackService : ITrackService
    {
        private readonly ITrackStore _store;
        private readonly IIdempotency _idempotencyStore;

        // Keeps the contribution check and write together
        // while using the in-memory implementation.
        private readonly SemaphoreSlim _contributionLock = new(1, 1);

        public TrackService(
            ITrackStore store,
            IIdempotency idempotencyStore)
        {
            _store = store;
            _idempotencyStore = idempotencyStore;
        }

        public async Task<User> AddMemberAsync(Guid stokvelId, Guid userId)
            {
                var stokvel =
                    await _store.GetStokvelByIdAsync(stokvelId);

                if (stokvel is null)
                {
                    throw new ResourceNotFoundException(
                        "The stokvel was not found.");
                }

                var user =
                    await _store.GetUserByIdAsync(userId);

                if (user is null)
                {
                    throw new ResourceNotFoundException(
                        "The user was not found.");
                }

                // Relationship business rule.
                if (stokvel.HasMember(userId))
                {
                    throw new DuplicateMembershipException();
                }

                stokvel.AddMember(user);

                await _store.UpdateStokvelAsync(stokvel);

                return user;
            }

            public async Task RemoveMemberAsync(Guid stokvelId,Guid userId)
            {
                var stokvel =
                    await _store.GetStokvelByIdAsync(stokvelId);

                if (stokvel is null)
                {
                    throw new ResourceNotFoundException(
                        "The stokvel was not found.");
                }

                var removed =
                    stokvel.RemoveMember(userId);

                if (!removed)
                {
                    throw new ResourceNotFoundException(
                        "The user is not a member of this stokvel.");
                }

                await _store.UpdateStokvelAsync(stokvel);
            }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user =
                await _store.GetUserByIdAsync(userId);

            if (user is null)
            {
                throw new ResourceNotFoundException(
                    "The user was not found.");
            }

            var belongsToStokvel =
                await _store
                    .IsUserMemberOfAnyStokvelAsync(userId);

            if (belongsToStokvel)
            {
                throw new ConflictException(
                    "The user cannot be deleted while they are still a member of a stokvel.");
            }

            await _store.DeleteUserAsync(userId);
        }

        public async Task<Contribution> RecordContributionAsync(
                Guid stokvelId,
                Guid userId,
                Guid contributionCycleId,
                decimal amount,
                string idempotencyKey)
            {
                var signature =
                    new ContributionRequestSignature(
                        stokvelId,
                        userId,
                        contributionCycleId,
                        amount);

                await _contributionLock.WaitAsync();

                try
                {
                    var previous =
                        await _idempotencyStore
                            .GetAsync(idempotencyKey);

                    if (previous is not null)
                    {
                        // Same key must always represent the same request.
                        if (previous.Signature != signature)
                        {
                            throw new IdempotencyConflictException();
                        }

                        // Safe retry: return the original payment.
                        return previous.Contribution;
                    }

                    var stokvel =
                        await _store.GetStokvelByIdAsync(stokvelId);

                    if (stokvel is null)
                    {
                        throw new ResourceNotFoundException("The stokvel was not found.");
                    }

                    var user =
                        await _store.GetUserByIdAsync(userId);

                    if (user is null)
                    {
                        throw new ResourceNotFoundException("The user was not found.");
                    }

                    var cycle = await _store
                        .GetContributionCycleByIdAsync(
                            stokvelId,
                            contributionCycleId);

                    if (cycle is null)
                    {
                        throw new ResourceNotFoundException("The contribution cycle was not found.");
                    }

                    if (!stokvel.HasMember(userId))
                    {
                        throw new BusinessRuleException(
                        "The user is not a member of this stokvel.");
                    }

                    if (amount != stokvel.MonthlyContribution)
                    {
                        throw new BusinessRuleException(
                         $"The required contribution is R{stokvel.MonthlyContribution:0.00}.");
                    }

                    var existingContribution =
                      await _store.GetContributionAsync(
                        stokvelId,
                        userId,
                        contributionCycleId);

                    if (existingContribution is not null)
                    {
                        throw new DuplicateContributionException(); 
                    }

                    var contribution =
                        new Contribution(
                        stokvelId,
                        userId,
                        contributionCycleId,
                        amount);

                   await _store.AddContributionAsync(contribution);

                    await _idempotencyStore.SaveAsync(
                    idempotencyKey,
                    new IdempotencyRecord(
                        signature,
                        contribution));

                   return contribution;
                }
                      finally
                    {
                        _contributionLock.Release();
                    }
    }
}