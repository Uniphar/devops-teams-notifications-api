using Microsoft.ApplicationInsights;

namespace Teams.Notifications.Api.Telemetry;

public static class NamedTelemetryExtensions
{
    public static void TrackChannelUpdate(this TelemetryClient telemetry, string teamName, string channelName, string id) =>
        telemetry.TrackEvent("ChannelUpdate",
            new
            {
                Team = teamName, Channel = channelName, Id = id
            });
}