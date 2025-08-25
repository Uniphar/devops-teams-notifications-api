using IMiddleware = Microsoft.Agents.Builder.IMiddleware;

namespace Teams.Notifications.Api.Middlewares;

// can be used to capture ALL data from the bot
public class CaptureMiddleware : IMiddleware
{
    public Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new())
    {
        return next(cancellationToken);
    }
}