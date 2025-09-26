using AdaptiveCard = AdaptiveCards.AdaptiveCard;

namespace Teams.Notifications.Api.Extensions;

public static class ChatMessageFinder
{
    public static string? GetAdaptiveCardFromChatMessage(this ChatMessage chatMessage)
    {
        // quick skip, we don't want to give back a removed item
        return chatMessage.DeletedDateTime != null
            ? null
            : chatMessage.Attachments?.FirstOrDefault(attachment => !string.IsNullOrWhiteSpace(attachment.Content))?.Content;
    }

    public static ChatMessage? GetCardThatHas(this ChatMessage chatMessage, string jsonFileName, string uniqueId)
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
            // we set a unique id so we can find it back
            var itemsWithUnique = card.Body.Where(x => x.Id == uniqueId);
            foreach (var itemWithUnique in itemsWithUnique)
            {
                // need to make sure it is a text block, with the json filename as text
                if (itemWithUnique is not AdaptiveTextBlock adaptiveTextBlock) continue;
                if (adaptiveTextBlock.Text == jsonFileName)
                    return chatMessage;
            }
        }

        return null;
    }
}