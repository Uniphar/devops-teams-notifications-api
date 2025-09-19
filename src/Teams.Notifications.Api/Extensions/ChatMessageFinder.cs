namespace Teams.Notifications.Api.Extensions;

public static class ChatMessageFinder
{
    public static AdaptiveCard? GetCardThatHas(this ChatMessage chatMessage, string jsonFileName, string uniqueId)
    {
        // quick skip, we don't want to give back a removed item
        if (chatMessage.DeletedDateTime != null) return null;
        var attachments = chatMessage.Attachments;
        if (attachments == null) return null;
        if (attachments.Count == 0) return null;
        if (attachments.Any(a => a.Content == null)) return null;
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.Content)) continue;
            var card = AdaptiveCard.FromJson(attachment.Content).Card;
            var itemWithUnique = card.Body.FirstOrDefault(x => x.Id == uniqueId);
            if (itemWithUnique is not AdaptiveTextBlock adaptiveTextBlock) continue;
            if (adaptiveTextBlock.Text == jsonFileName)
                return card;
        }

        return null;
    }
}