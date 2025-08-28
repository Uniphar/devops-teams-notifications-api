using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;

namespace Teams.Notifications.Api.Tests.Helpers;

public class TestTelemetryChannel : ITelemetryChannel
{
    public ConcurrentBag<ITelemetry> Sent { get; } = new();
    public void Send(ITelemetry item) => Sent.Add(item);
    public void Flush() { }
    public void Dispose() { }
    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }
}