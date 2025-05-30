using Teams.Notifications.AdaptiveCardGen;
using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Services;

public sealed class CardManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config) : ICardManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");
    private readonly string _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_TENANT_ID");

    public async Task DeleteCard(string jsonFileName, string uniqueId, string teamName, string channelName)
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName);
        await teamsManagerService.CheckBotIsInTeam(teamId);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName);
        var conversationReference = GetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageIdByUniqueId(teamId, channelId, jsonFileName, uniqueId);
        // check that we found the item to delete
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(uniqueId));
        conversationReference.ActivityId = id;
        // delete the item
        await adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            (turnContext, cancellationToken) => adapter.DeleteActivityAsync(turnContext, conversationReference, cancellationToken),
            CancellationToken.None);
    }

    public async Task CreateOrUpdate<T>(string jsonFileName, T model, string teamName, string channelName) where T : BaseTemplateModel
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName);
        await teamsManagerService.CheckBotIsInTeam(teamId);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName);
        var text = await File.ReadAllTextAsync($"./Templates/{jsonFileName}");
        var props = text.GetPropertiesFromJson();
        var fileUrl = string.Empty;
        if (props.HasFileTemplate())
        {
            var file = model.GetFileValue();
            if (file != null)
                fileUrl = await teamsManagerService.UploadFile(teamId, channelId, channelName + "/error/" + file.FileName, file.OpenReadStream());
        }

        // replace all props with the values
        foreach (var (propertyName, type) in props) text = text.FindPropAndReplace(model, propertyName, type, fileUrl);

        var item = AdaptiveCard.FromJson(text).Card;
        if (item == null) throw new ArgumentNullException(nameof(jsonFileName));
        // some solution to be able to track a unique id across the channel
        item.Body.Add(new AdaptiveTextBlock(jsonFileName)
        {
            Color = AdaptiveTextColor.Accent,
            Size = AdaptiveTextSize.Small,
            Id = model.UniqueId,
            IsSubtle = true,
            IsVisible = false,
            Wrap = true
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
        var conversationReference = GetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageIdByUniqueId(teamId, channelId, jsonFileName, model.UniqueId);
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