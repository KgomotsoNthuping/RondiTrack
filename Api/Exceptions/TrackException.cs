namespace Api.Exceptions;

// Base exception for expected RondiTrack failures.
public abstract class TrackException : Exception
{
    protected TrackException(string message)
        : base(message)
    {
    }
}

// Used when a requested User, Stokvel, Cycle, etc. does not exist.
public sealed class ResourceNotFoundException : TrackException
{
    public ResourceNotFoundException(string message) : base(message)
    { 

    }
}

// Used when the request is valid, but a business rule does not allow it.
public class BusinessRuleException : TrackException
{
    public BusinessRuleException(string message): base(message)
    {

    }
}

// Used when the request conflicts with existing system state.
public class ConflictException : TrackException
{
    public ConflictException(string message): base(message)
    {

    }
}

// A member cannot pay for the same cycle twice.
public sealed class DuplicateContributionException : ConflictException
{
    public DuplicateContributionException()
        : base("This member's contribution has already been recorded for this cycle.")
    {

    }
}

// Same Idempotency-Key cannot represent two different requests.
public sealed class IdempotencyConflictException : ConflictException
{
    public IdempotencyConflictException()
        : base("This Idempotency-Key has already been used with a different request.")
    {

    }
}

// A User cannot join the same Stokvel twice.
public sealed class DuplicateMembershipException : ConflictException
{
    public DuplicateMembershipException()
        : base("The user is already a member of this stokvel.")
    {
        
    }
}