using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;

namespace Teams.Notifications.Api.Middlewares;

// can be used to capture ALL data from the bot
public class CaptureMiddleware : IMiddleware
{
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new())
    {
        await next(cancellationToken).ConfigureAwait(false);
    }
}