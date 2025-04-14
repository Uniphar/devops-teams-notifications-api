using Azure.Core;
using Azure.Identity;
using Microsoft.Graph.Beta;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                _defaultCredential = new ClientSecretCredential(clientId, tenantId, clientSecret);
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
    }
}
