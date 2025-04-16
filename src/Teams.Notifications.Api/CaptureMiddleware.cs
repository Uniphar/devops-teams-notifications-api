using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;

namespace Teams.Notifications.Api;

public class CaptureMiddleware : IMiddleware
{
    private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

    public CaptureMiddleware(ConcurrentDictionary<string, ConversationReference> conversationReferences) => _conversationReferences = conversationReferences;

    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new())
    {
        AddConversationReference(turnContext.Activity as Activity);
        await next(cancellationToken).ConfigureAwait(false);
    }

    private void AddConversationReference(Activity activity)
    {
        var conversationReference = activity.GetConversationReference();
        _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
    }
}