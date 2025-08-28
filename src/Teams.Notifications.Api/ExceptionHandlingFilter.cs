namespace Teams.Notifications.Api;

internal class ExceptionHandlingFilter : IExceptionFilter
{
    private readonly TelemetryClient _telemetry;

    public ExceptionHandlingFilter(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case NotImplementedException:
                _telemetry.TrackException(context.Exception);
                context.Result = new StatusCodeResult(StatusCodes.Status501NotImplemented);
                break;
            // any of the InvalidOperationException is considered a user messes up action, so we can do a 400 with the info
            case InvalidOperationException invalidOperation:
                _telemetry.TrackException(context.Exception);
                context.Result = new ObjectResult(invalidOperation.Message) { StatusCode = StatusCodes.Status400BadRequest };
                break;
            default:
                _telemetry.TrackException(context.Exception);
                // do nothing, let the default handler deal with it
                context.Result = context.Result;
                break;
        }
    }
}