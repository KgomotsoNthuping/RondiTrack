namespace Domain.Exceptions;

public sealed class InviteExpiredException : Exception
{
    public InviteExpiredException()
        : base("This invitation has expired or been revoked.")
    {
    }
}