using System.Linq;
using Microsoft.Graph.Beta.Models;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Extensions;

public static class ChatMessageFinder
{
    public static bool GetMessageThatHas(this ChatMessage chatMessage, FileErrorModel modelToFind)
    {
        // quick skip, we don't want to give back a removed item
        if (chatMessage.DeletedDateTime != null) return false;
        var attachments = chatMessage.Attachments;
        if (attachments == null) return false;
        if (attachments.Count == 0) return false;
        if (attachments.Any(a => a.Content == null)) return false;
        if (!attachments.Any(a => a.Content.Contains(modelToFind.System))) return false;
        if (!attachments.Any(a => a.Content.Contains(modelToFind.JobId))) return false;
        if (!attachments.Any(a => a.Content.Contains(modelToFind.FileName))) return false;

        return true;
    }
}