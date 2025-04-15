using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services;

public class TeamsChannelManagingService : ITeamsChannelManagingService
{
    private readonly GraphServiceClient _graphClient;

    public TeamsChannelManagingService(GraphServiceClient graphClient) => _graphClient = graphClient;

    public async Task<KeyValuePair<string, string>> GetTeamAndChannelId(string teamName, string channelName)
    {
        var groups = await _graphClient.Teams.GetAsync(request =>
        {
            request.QueryParameters.Filter = $"displayName eq '{teamName}'";
            request.QueryParameters.Select = ["id"];
        });

        if (groups is not { Value: [Team { Id: var teamId }] })
            throw new InvalidOperationException("Teams with displayName `{teamName}` does not exist");
        var channels = await _graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(request =>
            {
                request.QueryParameters.Filter = $"displayName eq '{channelName}'";
                request.QueryParameters.Select = ["id"];
            });

        if (channels is not { Value: [Channel { Id: var channelId }] })
            throw new InvalidOperationException("Teams with displayName `{teamName}` does not exist");

        return new KeyValuePair<string, string>(teamId, channelId);
    }
    public async Task UpdateFileErrorCard(FileErrorModel model,string teamId, string channelId, string messageId)
    {
        var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
        var requestBody = new ChatMessage
        {
            Subject = null,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = $"<attachment id=\"{guid}\"></attachment>"
            },
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Id = guid,
                    ContentType = AdaptiveCard.ContentType,
                    ContentUrl = null,
                    Content = AdaptiveCardBuilder.CreateFileProcessingCard(model).ToJson(),
                    Name = null,
                    ThumbnailUrl = null
                }
            ]
        };

        await _graphClient.Teams[teamId].Channels[channelId].Messages[messageId].PatchAsync(requestBody);
    }
    public async Task<string> CreateFileErrorCard(FileErrorModel model, string teamId, string channelId)
    {
        var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
        var requestBody = new ChatMessage
        {
            Subject = null,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = $"<attachment id=\"{guid}\"></attachment>"
            },
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Id = guid,
                    ContentType = AdaptiveCard.ContentType,
                    ContentUrl = null,
                    Content = AdaptiveCardBuilder.CreateFileProcessingCard(model).ToJson(),
                    Name = null,
                    ThumbnailUrl = null
                }
            ]
        };

        var result = await _graphClient.Teams[teamId].Channels[channelId].Messages.PostAsync(requestBody);
        return result?.ChatId ?? string.Empty;
    }

    public async Task DeleteFileErrorCard(string teamId, string channelId, string messageId)
    {
        await _graphClient.Teams[teamId].Channels[channelId].Messages[messageId].SoftDelete.PostAsync();
    }
}