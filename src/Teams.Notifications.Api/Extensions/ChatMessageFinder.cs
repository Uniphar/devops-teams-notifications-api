using System.Linq;
using Microsoft.Graph.Beta.Models;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services;

public static class ChatMessageFinder
{
    public static bool GetMessageThatHas(this ChatMessage s, FileErrorModel modelToFind)
    {
        // quick skip, we don't want to give back a removed item
        if (s.DeletedDateTime != null) return false;
        var attachments = s.Attachments;
        if (attachments == null) return false;
        if (attachments.Count == 0) return false;
        if (!attachments.Any(s => s.Content.Contains(modelToFind.System))) return false;
        if (!attachments.Any(s => s.Content.Contains(modelToFind.JobId))) return false;
        if (!attachments.Any(s => s.Content.Contains(modelToFind.FileName))) return false;

        return true;
    }
}