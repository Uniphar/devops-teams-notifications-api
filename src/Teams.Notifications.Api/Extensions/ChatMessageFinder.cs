namespace Teams.Notifications.Api.Extensions;

public static class ChatMessageFinder
{
    public static bool GetMessageThatHas(this ChatMessage chatMessage, string jsonFileName, string uniqueId)
    {
        // quick skip, we don't want to give back a removed item
        if (chatMessage.DeletedDateTime != null) return false;
        var attachments = chatMessage.Attachments;
        if (attachments == null) return false;
        if (attachments.Count == 0) return false;
        if (attachments.Any(a => a.Content == null)) return false;
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.Content)) continue;
            var card = AdaptiveCard.FromJson(attachment.Content).Card;
            var itemWithUnique = card.Body.FirstOrDefault(x => x.Id == uniqueId);
            if (itemWithUnique is not AdaptiveTextBlock adaptiveTextBlock) continue;
            if (adaptiveTextBlock.Text == jsonFileName)
                return true;
        }

        return false;
    }
}