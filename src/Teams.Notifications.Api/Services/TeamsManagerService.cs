using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services;

public class TeamsManagerService : ITeamsManagerService
{
    private readonly GraphServiceClient _graphClient;

    public TeamsManagerService(GraphServiceClient graphClient) => _graphClient = graphClient;

    public async Task<string> GetTeamIdAsync(string teamName)
    {
        var groups = await _graphClient.Teams.GetAsync(request =>
        {
            request.QueryParameters.Filter = $"displayName eq '{teamName}'";
            request.QueryParameters.Select = ["id"];
        });

        if (groups is not { Value: [Team { Id: var teamId }] })
            throw new InvalidOperationException("Teams with displayName `{teamName}` does not exist");
        return teamId;
    }

    public async Task<string> GetChannelIdAsync(string teamId, string channelName)
    {
        var channels = await _graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(request =>
            {
                request.QueryParameters.Filter = $"displayName eq '{channelName}'";
                request.QueryParameters.Select = ["id"];
            });

        if (channels is not { Value: [{ Id: var channelId }] })
            throw new InvalidOperationException("Teams with displayName `{teamName}` does not exist");
        return channelId;
    }
   
 
}
