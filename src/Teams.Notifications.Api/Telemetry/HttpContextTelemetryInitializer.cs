namespace Teams.Notifications.Api.Telemetry;

internal class HttpContextTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : ITelemetryInitializer
{
    public const string RequestPath = "UrlPath";
    private const string ResponseStatusCode = "StatusCode";

    public void Initialize(ITelemetry telemetry)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return;

        if (telemetry is not ISupportProperties propTelemetry) return;

        propTelemetry.Properties.TryGetValue(ResponseStatusCode, out var statusCode);
        if (string.IsNullOrWhiteSpace(statusCode)) propTelemetry.Properties[ResponseStatusCode] = context.Response.StatusCode.ToString();

        propTelemetry.Properties.TryGetValue(RequestPath, out var urlPath);
        if (string.IsNullOrWhiteSpace(urlPath)) propTelemetry.Properties[RequestPath] = context.Request.Path;
    }
}