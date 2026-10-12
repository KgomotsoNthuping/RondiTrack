namespace Api.Services;

public interface IPayoutTransaction
{
    Task AfterPayoutSavedAsync();
}

public class NoOpPayoutTransaction : IPayoutTransaction
{
    public Task AfterPayoutSavedAsync()
    {
        return Task.CompletedTask;
    }
}