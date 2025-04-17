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
     
        var requestBody = new ChatMessage
        {
            Subject = null,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = $"<attachment id=\"{model.GetHashCode()}\"></attachment>"
            },
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Id = model.GetHashCode().ToString(),
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
    
    public async Task DeleteFileErrorCard(string teamId, string channelId, string messageId)
    {
        await _graphClient.Teams[teamId].Channels[channelId].Messages[messageId].SoftDelete.PostAsync();
    }
}