using System;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services;

public class TeamsChannelMessagingService : ITeamsChannelMessagingService
{
    private readonly GraphServiceClient _graphClient;

    public TeamsChannelMessagingService(GraphServiceClient graphClient) => _graphClient = graphClient;

   
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
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = $"<attachment id=\"{guid}\"></attachment>"
            },
            From = new ChatMessageFromIdentitySet
            {
                Application = new TeamworkApplicationIdentity
                {
                    OdataType = "#microsoft.graph.teamworkApplicationIdentity",
                    Id = "4c1c145e-eb0d-47cd-a2f1-02df93cf2c92",
                    ApplicationIdentityType = TeamworkApplicationIdentityType.Bot
                }
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
                    ThumbnailUrl = null,
                    TeamsAppId = "4c1c145e-eb0d-47cd-a2f1-02df93cf2c92"
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