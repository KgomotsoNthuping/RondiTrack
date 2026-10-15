using System.Diagnostics.Metrics;

namespace Api.Observability;

public sealed class CycleCloseMetrics
{
    public const string MeterName =
        "RondiTrack.Jobs";

    private static readonly Meter Meter =
        new(MeterName);

    private readonly Counter<long>
        _cyclesClosed =
            Meter.CreateCounter<long>(
                "ronditrack.jobs.cycles_closed");

    private readonly Counter<long>
        _arrearsRecorded =
            Meter.CreateCounter<long>(
                "ronditrack.jobs.arrears_recorded");

    public void RecordCyclesClosed(
        int count)
    {
        if (count > 0)
        {
            _cyclesClosed.Add(count);
        }
    }

    public void RecordArrears(
        int count)
    {
        if (count > 0)
        {
            _arrearsRecorded.Add(count);
        }
    }
}