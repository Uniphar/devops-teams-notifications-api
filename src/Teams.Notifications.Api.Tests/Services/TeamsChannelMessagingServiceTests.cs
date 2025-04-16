using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Builder;
using Microsoft.Graph.Beta;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services;

namespace Teams.Notifications.Api.Tests.Services
{
    [TestClass]
    [TestCategory("Integration")]
    public sealed class TeamsChannelMessagingServiceTests
    {
        private static string _tenantId;
        private static string _clientId;
        private static FileErrorManagerService _fileErrorManager;
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
                _clientId = context.Properties["ClientId"]!.ToString();
                _tenantId = context.Properties["TenantId"]!.ToString();
                var clientSecret = context.Properties["ClientSecret"]!.ToString();
                _defaultCredential = new ClientSecretCredential(_tenantId, _clientId, clientSecret);
            }
            else
                _defaultCredential = new DefaultAzureCredential();


            var graph = new GraphServiceClient(_defaultCredential);
            _teamChannelService = new TeamsChannelMessagingService(graph);
            var adapter = new Mock<IChannelAdapter>();
           // _fileErrorManager = new FileErrorManagerService(adapter.Object);
            _teamManager = new TeamsManagerService(graph);
        }

        [TestMethod]
        public async Task BasicTeamChannelTest()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var teamId = await _teamManager.GetTeamIdAsync(teamName);
            var channelId = await _teamManager.GetChannelIdAsync(teamId, channelName);
            Assert.IsNotEmpty(teamId);
            Assert.IsNotEmpty(channelId);
        }

        [TestMethod]
        public async Task BasicTeamChannelAddCard()
        {
            var teamName = "Frontgate Files Moving Integration Test In";
            var channelName = "General";

            var teamId = await _teamManager.GetTeamIdAsync(teamName);
            var channelId = await _teamManager.GetChannelIdAsync(teamId, channelName);
            Assert.IsNotEmpty(teamId);
            Assert.IsNotEmpty(channelId);
            var fileError = new FileErrorModel
            {
                FileName = "Test",
                System = "test",
                JobId = "test",
                Status = FileErrorStatusEnum.Failed
            };
            await _fileErrorManager.CreateFileErrorCardAsync(fileError,  channelId);

            fileError.Status = FileErrorStatusEnum.Succes;
        }
    }
}
