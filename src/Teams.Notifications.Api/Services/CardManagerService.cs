using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Services;

public sealed class CardManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config, TelemetryClient telemetry) : ICardManagerService
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
            async (turnContext, cancellationToken) =>
            {
                await adapter.DeleteActivityAsync(turnContext, conversationReference, cancellationToken);
                telemetry.TrackChannelDeleteMessage(teamName, channelName, conversationReference.ActivityId);
            },
            CancellationToken.None);
    }

    public async Task CreateOrUpdate<T>(string jsonFileName, T model, string teamName, string channelName) where T : BaseTemplateModel
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName);
        await teamsManagerService.CheckBotIsInTeam(teamId);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName);

        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = await CreateCardFromTemplateAsync(jsonFileName, model, teamsManagerService, teamId, channelId, channelName)
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
            async (turnContext, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(activity.Id))
                {
                    // item is new
                    var newResult = await turnContext.SendActivityAsync(activity, cancellationToken);
                    telemetry.TrackChannelNewMessage(teamName, channelName, newResult.Id);
                }

                var updateResult = await turnContext.UpdateActivityAsync(activity, cancellationToken);
                telemetry.TrackChannelUpdateMessage(teamName, channelName, updateResult.Id);
                // item needs update
            },
            CancellationToken.None);
    }

    public static async Task<string> CreateCardFromTemplateAsync<T>(string jsonFileName, T model, ITeamsManagerService teamsManagerService, string teamId, string channelId, string channelName) where T : BaseTemplateModel
    {
        var text = await File.ReadAllTextAsync($"./Templates/{jsonFileName}");
        var props = text.GetMustachePropertiesFromString();
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
        return item.ToJson();
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