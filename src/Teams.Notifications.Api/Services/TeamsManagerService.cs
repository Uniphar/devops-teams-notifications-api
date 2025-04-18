using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;
using Teams.Notifications.Api.Extensions;
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
        return teamId ?? throw new InvalidOperationException();
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
        return channelId ?? throw new InvalidOperationException();
    }

    public async Task<string?> GetMessageId(string teamId, string channelId, FileErrorModel modelToFind)
    {
        // we have to get the full thing since select or filter is not allowed, but we can request 100 messages at a time
        var response = await _graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(x => x.QueryParameters.Top = 100);
        // no need to do anything if there is no message
        if (response?.Value == null) return string.Empty;
        var id = response.Value.FirstOrDefault(s => s.GetMessageThatHas(modelToFind))?.Id;
        if (!string.IsNullOrWhiteSpace(id))
            return id;
        while (response?.OdataNextLink != null)
        {
            var configuration = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri(response.OdataNextLink)
            };

            response = await _graphClient.RequestAdapter.SendAsync(configuration, _ => new ChatMessageCollectionResponse());
            if (response?.Value == null) throw new Exception("Messages should not be null if there is a next page");
            id = response.Value.FirstOrDefault(s => s.GetMessageThatHas(modelToFind))?.Id;
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return id;
    }
}