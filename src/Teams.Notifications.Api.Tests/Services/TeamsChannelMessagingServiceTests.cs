using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Builder;
using Microsoft.Graph.Beta;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services;

namespace Teams.Notifications.Api.Tests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class TeamsChannelMessagingServiceTests
{
    private static string _tenantId = null!;
    private static string _clientId = null!;
    private static readonly FileErrorManagerService _fileErrorManager = null!;
    private static TeamsManagerService _teamManager = null!;
    private static TeamsChannelMessagingService _teamChannelService = null!;
    private static TokenCredential _defaultCredential = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var environment = context.Properties["Environment"]!.ToString();
        if (environment == "local")
        {
            // Values from app registration
            _clientId = context.Properties["ClientId"]?.ToString() ?? throw new ArgumentNullException(nameof(context));
            _tenantId = context.Properties["TenantId"]?.ToString() ?? throw new ArgumentNullException(nameof(context));
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
        const string teamName = "Frontgate Files Moving Integration Test In";
        const string channelName = "General";

        var teamId = await _teamManager.GetTeamIdAsync(teamName);
        var channelId = await _teamManager.GetChannelIdAsync(teamId, channelName);
        Assert.IsNotEmpty(teamId);
        Assert.IsNotEmpty(channelId);
    }

    [TestMethod]
    public async Task BasicTeamChannelAddCard()
    {
        const string teamName = "Frontgate Files Moving Integration Test In";
        const string channelName = "General";

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
        await _fileErrorManager.CreateUpdateOrDeleteFileErrorCardAsync(fileError, channelId);

        fileError.Status = FileErrorStatusEnum.Succes;
    }
}