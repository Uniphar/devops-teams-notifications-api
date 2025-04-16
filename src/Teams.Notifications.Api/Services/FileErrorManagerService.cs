using System.Collections.Generic;
using System.Threading;
using Microsoft.Agents.Builder;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Agents.Core.Models;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services
{
    public class FileErrorManagerService : IFileErrorManagerService
    {
        private readonly ICardStatesService _cardStatesService;
        private readonly ITeamsChannelMessagingService _channelMessagingService;
        private readonly IChannelAdapter _adapter;
        private readonly ITeamsManagerService _teamsManagerService;

        public FileErrorManagerService(ICardStatesService cardStatesService, ITeamsManagerService teamsManagerService, IChannelAdapter adapter)
        {
            _cardStatesService = cardStatesService;
            _teamsManagerService = teamsManagerService;
            _adapter = adapter;
        }

        public async Task CreateFileErrorCard(FileErrorModel fileError)
        {
            var tenantId = "8421dd92-337e-4405-8cfc-16118ffc5715";
            var clientId = "e50979f1-e66c-48fe-bdd9-ff0f634acc1";
            var teamChannelId = "19:19a98T0aX1b-w0aZgSDNOG6pkNkT0nkDmgHeKfvhBCk1@thread.tacv2";


            var json = AdaptiveCardBuilder.CreateFileProcessingCard(fileError).ToJson();
            // Create a response message based on the response content type from the WeatherForecastAgent
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
            var refe = new ConversationReference
            {
                ChannelId = Channels.Msteams,
                ServiceUrl = $"https://smba.trafficmanager.net/emea/{tenantId}",
                Conversation = new ConversationAccount(id: teamChannelId),
                ActivityId = teamChannelId
            };
            await _adapter.ContinueConversationAsync(clientId,
                refe,
                async (turnContext, cancellationToken) => { await turnContext.SendActivityAsync(activity, cancellationToken: cancellationToken); },
                CancellationToken.None);
        }
      

        public async Task UpdateFileErrorCard(int id, FileErrorModel fileError) => throw new System.NotImplementedException();

        public async Task DeleteFileErrorCard(string id) => throw new System.NotImplementedException();
    }
}
