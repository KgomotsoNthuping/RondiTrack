namespace Domain.Exceptions;

public sealed class InvitePermissionException : Exception
{
    public InvitePermissionException()
        : base("Only a Treasurer may create this invitation.")
    {
    }
}