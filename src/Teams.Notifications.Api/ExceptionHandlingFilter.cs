using Microsoft.AspNetCore.Mvc.Filters;

namespace Teams.Notifications.Api;
internal class ExceptionHandlingFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            NotImplementedException => new StatusCodeResult(StatusCodes.Status501NotImplemented),
            // any of the InvalidOperationException is considered a user messes up action, so we can do a 400 with the info
            InvalidOperationException invalidOperation => 
                new ObjectResult(invalidOperation.Message) { StatusCode = StatusCodes.Status400BadRequest },
        };
    }
}