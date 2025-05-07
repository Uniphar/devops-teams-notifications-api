using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Teams.Notifications.Api.Telemetry;

public class TelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
	public void Process(ITelemetry item)
	{
		if (item is not ISupportProperties telemetryItem)
		{
			next.Process(item);
			return;
		}

		if (telemetryItem.Properties.TryGetValue(HttpContextTelemetryInitializer.RequestPath, out string? requestPath))
		{
			if (requestPath.EndsWith("/health"))
			{
				return;
			}
		}

		next.Process(item);
	}
}
