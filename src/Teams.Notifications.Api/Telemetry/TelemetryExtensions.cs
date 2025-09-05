using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using KeyValuePair = System.Collections.Generic.KeyValuePair;

namespace Teams.Notifications.Api.Telemetry;

internal static class TelemetryExtensions
{
    public static AmbientTelemetryProperties WithProperties(this ICustomEventTelemetryClient telemetry, IEnumerable<KeyValuePair<string, string>> properties) => AmbientTelemetryProperties.Initialize(properties);

    public static AmbientTelemetryProperties WithProperties(this ICustomEventTelemetryClient telemetry, object properties) => AmbientTelemetryProperties.Initialize(properties.GrabProperties());

    public static AmbientTelemetryProperties WithProperty(this ICustomEventTelemetryClient telemetry, string name, string value) => AmbientTelemetryProperties.Initialize([KeyValuePair.Create(name, value)]);

    /// <summary>
    ///     Send an <see cref="ICustomEventTelemetryClient" /> for display in Diagnostic Search and in the Analytics
    ///     Portal.
    /// </summary>
    /// <param name="telemetry">The telemetry client.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="properties">An anonymous object whose properties will be stringified and added to the event.</param>
    public static void TrackEvent(this ICustomEventTelemetryClient telemetry, string eventName, object properties) => telemetry.TrackEvent(eventName, properties.GrabProperties()!);

    public static void TrackEvent(this ICustomEventTelemetryClient telemetry, string eventName) => telemetry.TrackEvent(eventName);

    public static void TrackError(this ICustomEventTelemetryClient telemetry, string error, object properties) => telemetry.TrackException(new Exception(error), properties.GrabProperties()!);

    public static void TrackError(this ICustomEventTelemetryClient telemetry, Exception ex, string eventName, object properties)
    {
        telemetry.TrackEvent(eventName);
        telemetry.TrackException(ex, properties.GrabProperties()!);
    }

    public static void RegisterOpenTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            // Filter out health checks
            options.Filter = httpContext => !httpContext.Request.Path.Value?.Contains("health") ?? true;
            options.RecordException = true;
        });
        builder.Services.AddSingleton<ICustomEventTelemetryClient, CustomEventTelemetryClient>();
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddTelemetrySdk()
            .AddService(serviceName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
                ["host.name"] = Environment.MachineName,
                ["os.description"] = RuntimeInformation.OSDescription,
                ["environment"] = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "dev",
                ["deployment.environment"] = builder.Configuration["DEPLOYMENT_ENVIRONMENT"] ?? "dev"
            });

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
        });

        var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS:CONNECTIONSTRING"];
        builder
            .Services
            .AddOpenTelemetry()
            .UseAzureMonitor(options => { options.ConnectionString = appInsightsConnectionString; })
            .WithTracing(x =>
            {
                x.AddSource(serviceName);

#if LOCAL || DEBUG
                x.AddConsoleExporter();
                //no sampling in local environment
                x.SetSampler(new AlwaysOnSampler());
#endif
            })
            .WithLogging(x => x.AddProcessor<CustomEventLogRecordProcessor>())
            .WithMetrics(x => x
                .SetResourceBuilder(resourceBuilder)
                .AddMeter(serviceName)
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
            );

        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                // Inject custom properties specified from within actors (_telemetry.WithProperties)
                var activityTags = AmbientTelemetryProperties.AmbientProperties.SelectMany(p => p.PropertiesToInject);
                foreach (var (name, value) in activityTags) activity.SetTag(name, value);
            }
        };

        ActivitySource.AddActivityListener(listener);
    }

    public static Dictionary<string, object> ToDictionary(this object obj)
    {
        return obj
            .GetType()
            .GetProperties()
            // make sure you can read
            .Where(prop => prop.CanRead)
            // prevent TargetParameterCountException 
            .Where(prop => prop.GetIndexParameters().Length == 0)
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj) ?? string.Empty);
    }
}

internal sealed class AmbientTelemetryProperties : IDisposable
{
    private AmbientTelemetryProperties(IEnumerable<KeyValuePair<string, string>>? propertiesToInject) => PropertiesToInject = propertiesToInject?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;
    private static AsyncLocal<ImmutableList<AmbientTelemetryProperties>> AmbientPropertiesAsyncLocal { get; } = new();

    internal static ImmutableList<AmbientTelemetryProperties> AmbientProperties
    {
        get => AmbientPropertiesAsyncLocal.Value ?? ImmutableList<AmbientTelemetryProperties>.Empty;
        set => AmbientPropertiesAsyncLocal.Value = value;
    }

    internal ImmutableArray<KeyValuePair<string, string>> PropertiesToInject { get; }

    public void Dispose()
    {
        AmbientProperties = AmbientProperties.Remove(this);
    }

    public static AmbientTelemetryProperties Initialize(IEnumerable<KeyValuePair<string, string>>? propertiesToInject)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                // Inject custom properties (tags) into dependency
                var activityTags = AmbientProperties.SelectMany(p => p.PropertiesToInject);
                foreach (var (name, value) in activityTags) activity.SetTag(name, value);
            },
            ActivityStopped = _ => { }
        };

        ActivitySource.AddActivityListener(listener);


        var ambientProps = new AmbientTelemetryProperties(propertiesToInject);
        // Insert at the beginning of the list so that these props take precedence over existing ambient props
        AmbientProperties = AmbientProperties.Insert(0, ambientProps);
        return ambientProps;
    }
}

file static class AnonymousObjectSerializer
{
    private static ConcurrentDictionary<Type, Func<object, Dictionary<string, string>>> PropertyGrabbers { get; } = new();

    private static MethodInfo AddConditionalMethod { get; } = ReflectionExtensions.GetMethod(AddConditional);

    public static Dictionary<string, string>? GrabProperties(this object obj) =>
        obj switch
        {
            null => null,
            IEnumerable<KeyValuePair<string, string>> dict => dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            _ => GetPropertyGrabber(obj.GetType())(obj)
        };

    private static Func<object, Dictionary<string, string>> GetPropertyGrabber(Type type)
    {
        return PropertyGrabbers.GetOrAdd(type,
            static type =>
            {
                var objParam = Expression.Parameter(typeof(object), "obj");

                // var typedObj = (T)obj;
                var typedObj = Expression.Variable(type, "typedObj");
                var typedObjAssign = Expression.Assign(typedObj, Expression.Convert(objParam, type));

                // Dictionary<string, string> dictionary = new();
                var dictionary = Expression.Variable(typeof(Dictionary<string, string>), "dictionary");
                var instantiateDictionary = Expression.Assign(dictionary, Expression.New(typeof(Dictionary<string, string>)));

                var propertyDictionaryAddExpressions = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(prop =>
                    {
                        // dictionary.AddCondition("Property", typedObj.Property);
                        var getProperty = Expression.Property(typedObj, prop);
                        return Expression.Call(AddConditionalMethod, dictionary, Expression.Constant(prop.Name), Expression.Convert(getProperty, typeof(object)));
                    });

                var block = Expression.Block([typedObj, dictionary],
                [
                    typedObjAssign,
                    instantiateDictionary,
                    ..propertyDictionaryAddExpressions,
                    dictionary
                ]);

                return Expression.Lambda<Func<object, Dictionary<string, string>>>(block, objParam).Compile();
            });
    }

    private static void AddConditional(this Dictionary<string, string> dict, string key, object value)
    {
        var str = value switch
        {
            string s => s,
            int i => i.ToString("D", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c"),
            DateTime dt => dt.ToUniversalTime().ToString("O"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
            null => null,
            _ => value.ToString()
        };

        if (!string.IsNullOrWhiteSpace(str))
            dict.Add(key, str);
    }
}