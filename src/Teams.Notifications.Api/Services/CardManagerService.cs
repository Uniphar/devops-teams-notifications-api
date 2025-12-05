using AdaptiveCard = AdaptiveCards.AdaptiveCard;

namespace Teams.Notifications.Api.Services;

public sealed class CardManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config, ICustomEventTelemetryClient telemetry) : ICardManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");
    private readonly string _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_TENANT_ID");

    public async Task DeleteCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token)
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckOrInstallBotIsInTeam(teamId, token);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, token);
        var conversationReference = GetConversationReference(channelId);
        var id = await teamsManagerService.GetMessageIdByUniqueId(teamId, channelId, jsonFileName, uniqueId, token);
        // check that we found the item to delete
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(uniqueId));
        conversationReference.ActivityId = id;
        // delete the item
        await adapter.ContinueConversationAsync(AgentClaims.CreateIdentity(_clientId),
            conversationReference,
            async (turnContext, cancellationToken) =>
            {
                await adapter.DeleteActivityAsync(turnContext, conversationReference, cancellationToken);
                telemetry.TrackEvent("ChannelDeleteMessage",
                    new()
                    {
                        ["Team"] = teamName,
                        ["Channel"] = channelName,
                        ["Id"] = id
                    });
            },
            token);
    }

    public async Task<string?> GetCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token)
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckOrInstallBotIsInTeam(teamId, token);
        var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, token);
        var chatMessage = await teamsManagerService.GetMessageByUniqueId(teamId, channelId, jsonFileName, uniqueId, token);
        // check that we found the item to delete
        return chatMessage?.GetAdaptiveCardFromChatMessage();
    }

    public async Task CreateOrUpdateAsync<T>(string jsonFileName, T model, string user, CancellationToken token) where T : BaseTemplateModel
    {
        var userAadObjectId = await teamsManagerService.GetUserAadObjectIdAsync(user, token);
        var installedAppId = await teamsManagerService.GetOrInstallChatAppIdAsync(userAadObjectId, token);
        string? chatMessageJson = null;
        if (!string.IsNullOrWhiteSpace(installedAppId))
        {
            var chatId = await teamsManagerService.GetChatIdAsync(installedAppId, userAadObjectId, token);
            if (!string.IsNullOrWhiteSpace(chatId))
            {
                var chatMessage = await teamsManagerService.GetChatMessageByUniqueId(chatId, userAadObjectId, jsonFileName, model.UniqueId, token);
                chatMessageJson = chatMessage?.GetAdaptiveCardFromChatMessage();
            }
        }

        var audience = AgentClaims.GetTokenAudience(AgentClaims.CreateIdentity(_clientId));
        var conversationParam = new ConversationParameters
        {
            IsGroup = false,
            Members =
            [
                new(userAadObjectId, aadObjectId: userAadObjectId)
            ],
            Agent = new(_clientId, aadObjectId: _clientId),
            TenantId = _tenantId
        };
        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = await CreateCardFromTemplateAsync(jsonFileName, null, model, teamsManagerService, token: token)
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(chatMessageJson))
        {
            telemetry.TrackEvent("ExistingMessageFound");
        }

        await adapter.CreateConversationAsync(_clientId,
            Channels.Msteams,
            $"https://smba.trafficmanager.net/emea/{_tenantId}",
            audience,
            conversationParam,
            async (turnContext, cancellationToken) =>
            {
                var newResult = await turnContext.SendActivityAsync(activity, cancellationToken);
                telemetry.TrackEvent("NewMessage",
                    new()
                    {
                        ["MessageId"] = newResult.Id
                    });
            },
            token);
    }

    public async Task CreateOrUpdateAsync<T>(string jsonFileName, IFormFile? file, T model, string teamName, string channelName, CancellationToken token) where T : BaseTemplateModel
    {
        var teamId = await teamsManagerService.GetTeamIdAsync(teamName, token);
        await teamsManagerService.CheckOrInstallBotIsInTeam(teamId, token);
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

        await adapter.ContinueConversationAsync(AgentClaims.CreateIdentity(_clientId),
            conversationReference,
            async (turnContext, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(idFromOldMessage))
                {
                    // item is new
                    var newResult = await turnContext.SendActivityAsync(activity, cancellationToken);
                    telemetry.TrackEvent("ChannelNewMessage",
                        new()
                        {
                            ["Team"] = teamName,
                            ["Channel"] = channelName,
                            ["MessageId"] = newResult.Id
                        });
                    return;
                }

                // item needs update
                var updateResult = await turnContext.UpdateActivityAsync(activity, cancellationToken);
                telemetry.TrackEvent("ChannelUpdateMessage",
                    new()
                    {
                        ["Team"] = teamName,
                        ["Channel"] = channelName,
                        ["MessageId"] = updateResult.Id
                    });
            },
            token);
    }

    public static async Task<string> CreateCardFromTemplateAsync<T>(string jsonFileName, IFormFile? formFile, T model, ITeamsManagerService teamsManagerService, string? teamId = null, string? channelId = null, string? channelName = null, CancellationToken token = default) where T : BaseTemplateModel
    {
        var text = await File.ReadAllTextAsync($"./Templates/{jsonFileName}", token);
        var props = text.GetMustachePropertiesFromString();
        var fileUrl = string.Empty;
        var fileLocation = string.Empty;
        var fileName = string.Empty;
        if (!string.IsNullOrEmpty(teamId) && !string.IsNullOrEmpty(channelId))
        {
            if (props.HasFileTemplate() && formFile != null)
            {
                fileName = formFile.FileName;
                fileLocation = channelName + "/error/" + formFile.FileName;
                await using var stream = formFile.OpenReadStream();
                await teamsManagerService.UploadFile(teamId, channelId, fileLocation, stream, token);
                fileUrl = await teamsManagerService.GetFileUrl(teamId, channelId, fileLocation, token);
            }
        }

        // replace all props with the values

        foreach (var (propertyName, type) in props)
        {
            text = text.FindPropAndReplace(model,
                new()
                {
                    Property = propertyName,
                    Type = type,
                    File = new()
                    {
                        Url = fileUrl,
                        Location = fileLocation,
                        Name = fileName
                    }
                });
        }

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
            Conversation = new(id: channelId),
            ActivityId = channelId
        };
}