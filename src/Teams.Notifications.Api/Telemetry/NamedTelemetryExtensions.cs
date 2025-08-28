namespace Teams.Notifications.Api.Telemetry;

public static class NamedTelemetryExtensions
{
    public static void TrackChannelUpdateMessage(this TelemetryClient telemetry, string teamName, string channelName, string id) =>
        telemetry.TrackEvent("ChannelUpdateMessage",
            new
            {
                Team = teamName, Channel = channelName, Id = id
            });

    public static void TrackChannelNewMessage(this TelemetryClient telemetry, string teamName, string channelName, string id) =>
        telemetry.TrackEvent("ChannelNewMessage",
            new
            {
                Team = teamName, Channel = channelName, Id = id
            });

    public static void TrackChannelDeleteMessage(this TelemetryClient telemetry, string teamName, string channelName, string id) =>
        telemetry.TrackEvent("ChannelDeleteMessage",
            new
            {
                Team = teamName, Channel = channelName, Id = id
            });
}