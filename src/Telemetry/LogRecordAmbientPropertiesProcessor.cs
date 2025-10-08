using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Telemetry;

/// <summary>
///     Service to inject ambient telemetry properties into LogRecord
/// </summary>
public class LogRecordAmbientPropertiesProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        var activityTags = AmbientTelemetryProperties
            .AmbientProperties
            .SelectMany(x => x.PropertiesToInject)
            .ToArray();

        //merge the existing attributes with ambient properties
        var newAttributes = (logRecord.Attributes ?? [])
            .Concat(activityTags.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .GroupBy(x => x.Key)
            .Select(x => x.First())
            .ToList();
        logRecord.Attributes = newAttributes;


        base.OnEnd(logRecord);
    }
}