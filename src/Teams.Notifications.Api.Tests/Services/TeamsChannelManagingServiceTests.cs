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
    public sealed class TeamsChannelManagingServiceTests
    {
        private static TeamsChannelManagingService _teamChannelService;
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
            _teamChannelService = new TeamsChannelManagingService(graph);
        }

        [TestMethod]
        public async Task BasicTeamChannelTest()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var team = await _teamChannelService.GetTeamAndChannelId(teamName, channelName);
            Assert.IsNotEmpty(team.Key);
            Assert.IsNotEmpty(team.Value);
        }

        [TestMethod]
        public async Task BasicTeamChannelAddCard()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var team = await _teamChannelService.GetTeamAndChannelId(teamName, channelName);
            var teamId = team.Key;
            var channelId = team.Value;
            Assert.IsNotEmpty(teamId);
            Assert.IsNotEmpty(channelId);
            var fileError = new FileErrorModel
            {
                FileName = "Test",
                System = "test",
                JobId = "test",
                Status = FileErrorStatusEnum.Failed
            };
            var messageId = await _teamChannelService.CreateFileErrorCard(fileError, teamId, channelId);
            Assert.IsNotEmpty(messageId);
            fileError.Status = FileErrorStatusEnum.Succes;
            await _teamChannelService.UpdateFileErrorCard(fileError, teamId, channelId, messageId);
            //
            await _teamChannelService.DeleteFileErrorCard(teamId, channelId, messageId);
        }
    }
}
