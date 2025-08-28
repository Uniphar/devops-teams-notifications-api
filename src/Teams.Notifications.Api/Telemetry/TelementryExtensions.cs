using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.ApplicationInsights;
using KeyValuePair = System.Collections.Generic.KeyValuePair;

namespace Teams.Notifications.Api.Telemetry;

internal static class TelemetryExtensions
{
    public static IServiceCollection AddAmbientTelemetryProperties(this IServiceCollection services) => services.AddSingleton<ITelemetryInitializer, AmbientTelemetryProperties.Initializer>();

    public static AmbientTelemetryProperties WithProperties(this TelemetryClient telemetry, IEnumerable<KeyValuePair<string, string>> properties) => AmbientTelemetryProperties.Initialize(properties);

    public static AmbientTelemetryProperties WithProperties(this TelemetryClient telemetry, object properties) => AmbientTelemetryProperties.Initialize(properties.GrabProperties());

    public static AmbientTelemetryProperties WithProperty(this TelemetryClient telemetry, string name, string value) => AmbientTelemetryProperties.Initialize([KeyValuePair.Create(name, value)]);

    /// <summary>Send an <see cref="EventTelemetry" /> for display in Diagnostic Search and in the Analytics Portal.</summary>
    /// <param name="telemetry">The telemetry client.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="properties">An anonymous object whose properties will be stringified and added to the event.</param>
    public static void TrackEvent(this TelemetryClient telemetry, string eventName, object properties) => telemetry.TrackEvent(eventName, properties.GrabProperties());

    public static void TrackError(this TelemetryClient telemetry, string error, object properties) => telemetry.TrackException(new Exception(error), properties.GrabProperties());
}

internal sealed class AmbientTelemetryProperties : IDisposable
{
    private AmbientTelemetryProperties(IEnumerable<KeyValuePair<string, string>>? propertiesToInject) => PropertiesToInject = propertiesToInject?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;

    private static AsyncLocal<ImmutableList<AmbientTelemetryProperties>> AmbientPropertiesAsyncLocal { get; } = new();

    private static ImmutableList<AmbientTelemetryProperties> AmbientProperties
    {
        get => AmbientPropertiesAsyncLocal.Value ?? ImmutableList<AmbientTelemetryProperties>.Empty;
        set => AmbientPropertiesAsyncLocal.Value = value;
    }

    private ImmutableArray<KeyValuePair<string, string>> PropertiesToInject { get; }

    public void Dispose()
    {
        AmbientProperties = AmbientProperties.Remove(this);
    }

    public static AmbientTelemetryProperties Initialize(IEnumerable<KeyValuePair<string, string>>? propertiesToInject)
    {
        var ambientProps = new AmbientTelemetryProperties(propertiesToInject);
        // Insert at the beginning of the list so that these props take precedence over existing ambient props
        AmbientProperties = AmbientProperties.Insert(0, ambientProps);
        return ambientProps;
    }

    public sealed class Initializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is not ISupportProperties { Properties: var telemetryProperties })
                return;

            // Since we insert in reverse order, deeper/later calls to WithProperty take precedence,
            // but manually specifying any key at each tracking event always overrides any ambient value
            foreach (var propertiesToInject in AmbientProperties)
            foreach (var (name, value) in propertiesToInject.PropertiesToInject)
                telemetryProperties.TryAdd(name, value);
        }
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
            _ => value.ToString()
        };

        if (!string.IsNullOrWhiteSpace(str))
            dict.Add(key, str);
    }
}