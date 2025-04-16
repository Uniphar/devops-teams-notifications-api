using Azure.Core;
using Azure.Identity;
using Microsoft.Graph.Beta;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services;

namespace Teams.Notifications.Api.Tests.Services
{
    [TestClass]
    [TestCategory("Integration")]
    public sealed class TeamsChannelMessagingServiceTests
    {
        private static TeamsManagerService _teamManager;
        private static TeamsChannelMessagingService _teamChannelService;
        private static TokenCredential _defaultCredential;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            var environment = context.Properties["Environment"]!.ToString();
            if (environment == "local")
            {
                // Values from app registration
                var clientId = context.Properties["ClientId"]!.ToString();
                var tenantId = context.Properties["TenantId"]!.ToString();
                var clientSecret = context.Properties["ClientSecret"]!.ToString();
                _defaultCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            }
            else
                _defaultCredential = new DefaultAzureCredential();


            var graph = new GraphServiceClient(_defaultCredential);
            _teamChannelService = new TeamsChannelMessagingService(graph);
            _teamManager = new TeamsManagerService(graph);
        }

        [TestMethod]
        public async Task BasicTeamChannelTest()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var teamId = await _teamManager.GetTeamId(teamName);
            var channel = await _teamManager.GetChannelInfo(teamId, channelName);
            Assert.IsNotEmpty(teamId);
            Assert.IsNotEmpty(channel.Id);
        }

        [TestMethod]
        public async Task BasicTeamChannelAddCard()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var teamId = await _teamManager.GetTeamId(teamName);
            var channel = await _teamManager.GetChannelInfo(teamId, channelName);
            Assert.IsNotEmpty(teamId);
            Assert.IsNotEmpty(channel.Id);
            var fileError = new FileErrorModel
            {
                FileName = "Test",
                System = "test",
                JobId = "test",
                Status = FileErrorStatusEnum.Failed
            };
            var messageId = await _teamChannelService.CreateFileErrorCard(fileError, teamId, channel.Id);
            Assert.IsNotEmpty(messageId);
            fileError.Status = FileErrorStatusEnum.Succes;
            await _teamChannelService.UpdateFileErrorCard(fileError, teamId, channel.Id, messageId);
            //
            await _teamChannelService.DeleteFileErrorCard(teamId, channel.Id, messageId);
        }
    }
}
