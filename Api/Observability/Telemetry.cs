using System.Diagnostics;

namespace Api.Observability;

public static class Telemetry
{
    public const string ServiceName = "ronditrack-api";

    public const string ActivitySourceName = "RondiTrack";

    public const string MeterName = "RondiTrack";

    public static readonly ActivitySource
        ActivitySource = new(ActivitySourceName);
}