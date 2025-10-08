namespace Telemetry;

public interface ICustomEventTelemetryClient
{
    void TrackEvent(string eventName, object properties);
    void TrackEvent(string eventName);
}