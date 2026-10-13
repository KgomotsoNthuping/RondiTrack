using System.Diagnostics.Metrics;

namespace Api.Observability;

public class Metrics
{
    public const string MeterName = "RondiTrack";

    private readonly Counter<long> _contributionsRecorded;

    private readonly Counter<long> _payoutsProcessed;

    private readonly Counter<long> _authorizationRefusals;

    public Metrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _contributionsRecorded = meter.CreateCounter<long>("ronditrack.contributions.recorded");

        _payoutsProcessed = meter.CreateCounter<long>("ronditrack.payouts.processed");

        _authorizationRefusals = meter.CreateCounter<long>("ronditrack.authorization.refusals");
    }

    public void ContributionRecorded()
    {
        _contributionsRecorded.Add(1);
    }

    public void PayoutSucceeded()
    {
        _payoutsProcessed.Add(1,
            new KeyValuePair<string, object?>("outcome", "success"));
    }

    public void PayoutFailed()
    {
        _payoutsProcessed.Add(1,
            new KeyValuePair<string, object?>("outcome", "failure"));
    }

    public void AuthorizationNotFound()
    {
        _authorizationRefusals.Add(1,
            new KeyValuePair<string, object?>("result", "not_found"));
    }

    public void AuthorizationForbidden()
    {
        _authorizationRefusals.Add(1,
            new KeyValuePair<string, object?>("result", "forbidden"));
    }
}