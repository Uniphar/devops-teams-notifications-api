using OpenTelemetry;
using OpenTelemetry.Logs;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Teams.Notifications.Api.Telemetry;

public class CustomEventLogRecordProcessor(ICustomEventTelemetryClient eventTelemetryClient) : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        if (logRecord.LogLevel < LogLevel.Error)
            return;

        var exception = logRecord.Exception;

        // Filter out file locked exception and send as custom event
        if (exception is IOException && exception.Message.Contains("being used by another process"))
        {
            eventTelemetryClient.TrackEvent("LockedFile", new { Exception = exception.Message });

            // Suppress the original log
            logRecord.Attributes = [];
            logRecord.Body = string.Empty;
            logRecord.FormattedMessage = string.Empty;
            logRecord.CategoryName = string.Empty;
            logRecord.Exception = null;
            logRecord.LogLevel = LogLevel.None; // Set to None to prevent further processing
            return;
        }

        // Otherwise, let the log go through
        base.OnEnd(logRecord);
    }
}