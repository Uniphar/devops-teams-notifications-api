using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Services;

public sealed class CardManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config, ICustomEventTelemetryClient telemetry) : ICardManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");
    private readonly string _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_TENANT_ID");

    public async Task DeleteCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token)
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckBotIsInTeam(teamId, token);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, token);
        var conversationReference = GetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageIdByUniqueId(teamId, channelId, jsonFileName, uniqueId, token);
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
            token);
    }

    public async Task<string?> GetCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token)
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckBotIsInTeam(teamId, token);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, token);
        var chatMessage = await teamsManagerService.GetMessageByUniqueId(teamId, channelId, jsonFileName, uniqueId, token);
        // check that we found the item to delete
        return chatMessage?.GetAdaptiveCardFromChatMessage();
    }


    public async Task CreateOrUpdateAsync<T>(string jsonFileName, IFormFile? file, T model, string teamName, string channelName, CancellationToken token) where T : BaseTemplateModel
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckBotIsInTeam(teamId, token);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, token);

        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = await CreateCardFromTemplateAsync(jsonFileName, file, model, teamsManagerService, teamId, channelId, channelName, token)
                }
            }
        };
        var conversationReference = GetConversationReference(channelId);
        var idFromOldMessage = await teamsManagerService.GetMessageIdByUniqueId(teamId, channelId, jsonFileName, model.UniqueId, token);
        // found an existing card so update id
        if (!string.IsNullOrWhiteSpace(idFromOldMessage))
        {
            activity.Id = idFromOldMessage;
            conversationReference.ActivityId = idFromOldMessage;
        }

        await adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            async (turnContext, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(idFromOldMessage))
                {
                    // item is new
                    var newResult = await turnContext.SendActivityAsync(activity, cancellationToken);
                    telemetry.TrackChannelNewMessage(teamName, channelName, newResult.Id);
                    return;
                }

                // item needs update
                var updateResult = await turnContext.UpdateActivityAsync(activity, cancellationToken);
                telemetry.TrackChannelUpdateMessage(teamName, channelName, updateResult.Id);
            },
            token);
    }

    public static async Task<string> CreateCardFromTemplateAsync<T>(string jsonFileName, IFormFile? formFile, T model, ITeamsManagerService teamsManagerService, string teamId, string channelId, string channelName, CancellationToken token) where T : BaseTemplateModel
    {
        var text = await File.ReadAllTextAsync($"./Templates/{jsonFileName}", token);
        var props = text.GetMustachePropertiesFromString();
        var fileUrl = string.Empty;
        var fileLocation = string.Empty;
        var fileName = string.Empty;
        if (props.HasFileTemplate() && formFile != null)
        {
            fileName = formFile.FileName;
            fileLocation = channelName + "/error/" + formFile.FileName;
            await using var stream = formFile.OpenReadStream();
            await teamsManagerService.UploadFile(teamId, channelId, fileLocation, stream, token);
            stream.Close();
            fileUrl = await teamsManagerService.GetFileUrl(teamId, channelId, fileLocation, token);
        }

        // replace all props with the values

        foreach (var (propertyName, type) in props) text = text.FindPropAndReplace(model, propertyName, type, fileUrl, fileLocation, fileName);
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