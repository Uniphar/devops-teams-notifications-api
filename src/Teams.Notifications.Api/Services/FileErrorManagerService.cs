using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Configuration;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services;

public sealed class FileErrorManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config) : IFileErrorManagerService
{
    private readonly IChannelAdapter _adapter = adapter;
    private readonly ITeamsManagerService _teamsManagerService = teamsManagerService;
    private readonly string _clientId = config["ClientId"] ?? throw new ArgumentNullException(config["ClientId"]);
    private readonly string _tenantId = config["TenantId"] ?? throw new ArgumentNullException(config["TenantId"]);

    public async Task CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModel fileError, string teamId, string channelId)
    {
        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = AdaptiveCardBuilder.CreateFileProcessingCard(fileError).ToJson()
                }
            }
        };
        var conversationReference = new ConversationReference
        {
            ChannelId = Channels.Msteams,
            ServiceUrl = $"https://smba.trafficmanager.net/emea/{_tenantId}",
            Conversation = new ConversationAccount(id: channelId),
            ActivityId = channelId
        };
        var id = await _teamsManagerService.GetMessageId(teamId, channelId, fileError);
        if (!string.IsNullOrWhiteSpace(id))
        {
            activity.Id = id;
            conversationReference.ActivityId = id;
        }

        await _adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            async (turnContext, cancellationToken) =>
            {
                // success means we can remove the activity
                if (fileError.Status == FileErrorStatusEnum.Succes && !string.IsNullOrWhiteSpace(activity.Id))
                    await turnContext.DeleteActivityAsync(activity.Id, cancellationToken);
                // if we found and existing, update that item, only works if the item is not already removed
                if (!string.IsNullOrWhiteSpace(activity.Id))
                    await turnContext.UpdateActivityAsync(activity, cancellationToken);
                // create a new one
                else
                    await turnContext.SendActivityAsync(activity, cancellationToken);
            },
            CancellationToken.None);
    }
}