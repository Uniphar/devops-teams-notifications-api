using System.Text.Json;
using Teams.Notifications.AdaptiveCardGen;
using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Services;

public class CardManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config) : ICardManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(config["AZURE_CLIENT_ID"]);
    private readonly string _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(config["AZURE_TENANT_ID"]);


    public async Task CreateCard<T>(string jsonFileName, T model, string teamId, string channelId) where T : BaseTemplateModel
    {
        await CreateOrUpdate(jsonFileName, model, teamId, channelId);
    }

    public async Task UpdateCard<T>(string jsonFileName, T model, string teamId, string channelId) where T : BaseTemplateModel
    {
        await CreateOrUpdate(jsonFileName, model, teamId, channelId);
    }

    public async Task DeleteCard(string jsonFileName, string uniqueId, string teamId, string channelId)
    {
        var conversationReference = CetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageId(teamId, channelId, uniqueId);
        // check that we found the item to delete
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        // delete the item
        await adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            (turnContext, cancellationToken) => turnContext.DeleteActivityAsync(id, cancellationToken),
            CancellationToken.None);
    }

    private async Task CreateOrUpdate<T>(string jsonFileName, T model, string teamId, string channelId) where T : BaseTemplateModel
    {
        var text = await File.ReadAllTextAsync($"./Templates/{jsonFileName}");
        var props = text.GetPropertiesFromJson();
        // replace all props with the values
        foreach (var (propertyName, type) in props) text = text.FindPropAndReplace(model, propertyName, type);

        var item = AdaptiveCard.FromJson(text).Card;
        if (item == null) throw new ArgumentNullException(nameof(item));
        // some solution to be able to track an unique id across the channel
        item.Body.Add(new AdaptiveTextBlock("•")
        {
            Color = AdaptiveTextColor.Accent,
            Size = AdaptiveTextSize.Small,
            Id = model.UniqueId,
            IsSubtle = true,
            IsVisible = false,
            Wrap = true,
        });
        var jsonAdaptiveCard = item.ToJson();
        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = jsonAdaptiveCard
                }
            }
        };
        var conversationReference = CetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageId(teamId, channelId, model.UniqueId);
        // found an existing card so update id
        if (!string.IsNullOrWhiteSpace(id))
        {
            activity.Id = id;
            conversationReference.ActivityId = id;
        }

        await adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            (turnContext, cancellationToken) => string.IsNullOrWhiteSpace(activity.Id)
                // item is new
                ? turnContext.SendActivityAsync(activity, cancellationToken)
                :
                // item needs update
                turnContext.UpdateActivityAsync(activity, cancellationToken),
            CancellationToken.None);
    }

    private ConversationReference GetConversationReference(string channelId) =>
        new()
        {
            ChannelId = Channels.Msteams,
            ServiceUrl = $"https://smba.trafficmanager.net/emea/{_tenantId}",
            Conversation = new ConversationAccount(id: channelId),
            ActivityId = channelId
        };
   
}