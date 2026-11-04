namespace Domain.Exceptions;

public sealed class CycleClosePermissionException : Exception
{
    public CycleClosePermissionException()
        : base("Only a Treasurer of this stokvel may close its cycle.")
    {
    }
}