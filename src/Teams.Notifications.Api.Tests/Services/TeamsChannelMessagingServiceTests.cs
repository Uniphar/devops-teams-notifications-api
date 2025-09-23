﻿using Microsoft.Extensions.Configuration;
using Moq;
using KeyValuePair = System.Collections.Generic.KeyValuePair;

namespace Teams.Notifications.Api.Tests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class TeamsChannelMessagingServiceTests
{
    private static TeamsManagerService _teamManager;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var environment = context.Properties["Environment"]!.ToString();
        var clientId = context.Properties["ClientId"]?.ToString() ?? throw new ArgumentNullException(nameof(context));
        var graph = new GraphServiceClient(new DefaultAzureCredential());
        if (environment == "local")
        {
            // Values from app registration, for local purposes
            var tenantId = context.Properties["TenantId"]?.ToString() ?? throw new ArgumentNullException(nameof(context));
            var clientSecret = context.Properties["ClientSecret"]!.ToString();
            var defaultCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            graph = new GraphServiceClient(defaultCredential);
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                KeyValuePair.Create("AZURE_CLIENT_ID", clientId)!
            ])
            .Build();
        _teamManager = new TeamsManagerService(graph, configuration);
    }

    [TestMethod]
    public async Task BasicTeamChannelTest()
    {
        const string teamName = "Notifications Platform";
        const string channelName = "File Errors";

        var teamId = await _teamManager.GetTeamIdAsync(teamName, CancellationToken.None);
        var channelId = await _teamManager.GetChannelIdAsync(teamId, channelName, CancellationToken.None);
        Assert.IsNotEmpty(teamId);
        Assert.IsNotEmpty(channelId);
    }
}