using System.Diagnostics;

namespace Api.Observability;

public static class Correlation
{
    public static string GetId(
        HttpContext httpContext)
    {
        return Activity.Current?
            .TraceId
            .ToString()
            ?? httpContext.TraceIdentifier;
    }
}