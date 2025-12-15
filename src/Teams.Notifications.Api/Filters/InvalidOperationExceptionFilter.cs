using Microsoft.AspNetCore.Mvc.Filters;

namespace Teams.Notifications.Api.Filters;

public class InvalidOperationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidOperationException invalidOperationException) return;

        context.Result = new ObjectResult(new
        {
            error = "Invalid Operation",
            message = invalidOperationException.Message
        })
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

        context.ExceptionHandled = true;
    }
}