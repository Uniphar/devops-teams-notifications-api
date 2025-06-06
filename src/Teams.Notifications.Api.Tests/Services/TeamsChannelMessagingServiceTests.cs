﻿using Microsoft.Extensions.Configuration;

namespace Teams.Notifications.Api.Tests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class TeamsChannelMessagingServiceTests
{
    private static string _tenantId = null!;
    private static string _clientId = null!;
    private static TeamsManagerService _teamManager = null!;
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
        _teamManager = new TeamsManagerService(graph, new ConfigurationManager());
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
}