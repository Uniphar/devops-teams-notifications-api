namespace Teams.Notifications.Api.Telemetry;

internal class TelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (item is not ISupportProperties telemetryItem)
        {
            next.Process(item);
            return;
        }

        if (telemetryItem.Properties.TryGetValue(HttpContextTelemetryInitializer.RequestPath, out var requestPath))
            if (requestPath.EndsWith("/health", StringComparison.OrdinalIgnoreCase))
                return;

        next.Process(item);
    }
}