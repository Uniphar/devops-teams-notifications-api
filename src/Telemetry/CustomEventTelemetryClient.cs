using Microsoft.Extensions.Logging;

namespace Telemetry;

public class CustomEventTelemetryClient(ILogger<CustomEventTelemetryClient> logger) : ICustomEventTelemetryClient
{
    public const string CustomEventAttribute = "{microsoft.custom_event.name}";

    public void TrackEvent(string eventName, object state)
    {
        using (logger.BeginScope(state.ToDictionary()))
            //this is how OpenTelemetry tracks custom events in AppInsights
            //Note that it is logged as a critical event on purpose.
            //Otherwise, if you use the LogInformation, but LogLevel is set to Error it will not appear in AppInsights.
            logger.LogCritical(CustomEventAttribute, eventName);
    }

    public void TrackEvent(string eventName)
    {
        //this is how OpenTelemetry tracks custom events in AppInsights
        //Note that it is logged as a critical event on purpose.
        //Otherwise, if you use the LogInformation, but LogLevel is set to Error it will not appear in AppInsights.
        logger.LogCritical(CustomEventAttribute, eventName);
    }

    public void TrackMessageError(object? message)
    {
        throw new NotImplementedException();
    }
}