namespace Teams.Notifications.Api.Middlewares;

// can be used to capture ALL data from the bot
public class CaptureMiddleware : Microsoft.Agents.Builder.IMiddleware
{
    public Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new()) => next(cancellationToken);
}