using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Agents.Builder;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Configuration;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services
{
    public class FileErrorManagerService : IFileErrorManagerService
    {
        private readonly ITeamsChannelMessagingService _channelMessagingService;
        private readonly IChannelAdapter _adapter;
        private readonly string _clientId;
        private readonly string _tenantId;

        public FileErrorManagerService(IChannelAdapter adapter, IConfiguration config)
        {
            _adapter = adapter;
            _clientId = config["ClientId"] ?? throw new ArgumentNullException(config["ClientId"]);
            _tenantId = config["TenantId"] ?? throw new ArgumentNullException(config["TenantId"]);
        }

        public async Task<string> CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModel fileError, string teamChannelId)
        {
            if (fileError.Status == FileErrorStatusEnum.Succes) await DeleteFileErrorCard(fileError.GetId(), teamChannelId);
            var json = AdaptiveCardBuilder.CreateFileProcessingCard(fileError).ToJson();
            var activity = new Activity
            {
                Type = "message",
                Attachments = new List<Attachment>
                {
                    new()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = json
                    }
                }
            };
            var conversationReference = new ConversationReference
            {
                ChannelId = Channels.Msteams,
                ServiceUrl = $"https://smba.trafficmanager.net/emea/{_tenantId}",
                Conversation = new ConversationAccount(id: teamChannelId),
                ActivityId = teamChannelId
            };
            await _adapter.ContinueConversationAsync(_clientId,
                conversationReference,
                async (turnContext, cancellationToken) => { await turnContext.SendActivityAsync(activity, cancellationToken); },
                CancellationToken.None);
            return fileError.GetId();
        }
        
        public async Task DeleteFileErrorCard(string id, string teamChannelId) => throw new System.NotImplementedException();
    }
}
