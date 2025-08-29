namespace Teams.Notifications.Api.Telemetry;

public interface ICustomEventTelemetryClient
{
    void TrackEvent(string eventName, object properties);
    void TrackEvent(string eventName);
    void TrackException(Exception ex, object state);
    void TrackException(Exception ex);
}