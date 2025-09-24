using System.Threading;

namespace Teams.Notifications.Api.Services;

public class TeamsManagerService(GraphServiceClient graphClient, IConfiguration config) : ITeamsManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");

    public async Task CheckBotIsInTeam(string teamId, CancellationToken token)
    {
        var result = await graphClient
            .Teams[teamId]
            .InstalledApps
            .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Expand = ["teamsAppDefinition"];
                    requestConfiguration.QueryParameters.Filter = $"teamsAppDefinition/authorization/clientAppId eq '{_clientId}'";
                },
                token);
        if (result?.Value?.Count == 0) throw new InvalidOperationException("Please install the bot on the Team, it is not installed at the moment");
    }

    public async Task<string> GetTeamIdAsync(string teamName, CancellationToken token)
    {
        var groups = await graphClient.Teams.GetAsync(request =>
        {
            request.QueryParameters.Filter = $"displayName eq '{teamName}'";
            request.QueryParameters.Select = ["id"];
        },
        token);

        if (groups is not { Value: [{ Id: var teamId }] })
            throw new InvalidOperationException($"Team with name {teamName} does not exist");
        return teamId ?? throw new InvalidOperationException($"Team with name {teamName} does not exist");
    }

    public async Task<string> GetChannelIdAsync(string teamId, string channelName, CancellationToken token)
    {
        var channels = await graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(request =>
                {
                    request.QueryParameters.Filter = $"displayName eq '{channelName}'";
                    request.QueryParameters.Select = ["id"];
                },
                token);

        if (channels is not { Value: [{ Id: var channelId }] })
            throw new InvalidOperationException($"Channel with name {channelName} does not exist");
        return channelId ?? throw new InvalidOperationException($"Channel with name {channelName} does not exist");
    }

    public async Task<string> GetGroupNameUniqueName(string groupId, CancellationToken token)
    {
        // teamId and groupId is the same, but if you look up group from a team it won't work!
        var group = await graphClient
            .Groups[groupId]
            .GetAsync(cancellationToken: token);
        return group?.UniqueName ?? throw new InvalidOperationException($"No group found for team {groupId}");
    }

    public async Task<string> GetTeamName(string teamId, CancellationToken token)
    {
        var team = await graphClient
            .Teams[teamId]
            .GetAsync(cancellationToken: token);
        return team?.DisplayName ?? throw new InvalidOperationException($"No DisplayName found for team {teamId}");
    }

    public async Task<string?> GetMessageIdByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId, CancellationToken token) => (await GetMessageByUniqueId(teamId, channelId, jsonFileName, uniqueId, token))?.Id;

    public async Task<ChatMessage?> GetMessageByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId, CancellationToken token)
    {
        // we have to get the full thing since select or filter is not allowed, but we can request 100 messages at a time
        var response = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(x => { x.QueryParameters.Top = 100; }, token);
        var responses = response
            ?.Value
            ?.Where(x => x.DeletedDateTime == null &&
                         x.From?.Application != null &&
                         x.From.Application.Id == _clientId
            )
            .ToList();
        // no need to do anything if there is no message
        if (responses == null) return null;
        var foundMessage = responses.Select(s => s.GetCardThatHas(jsonFileName, uniqueId)).FirstOrDefault(x => x != null);
        if (foundMessage != null)
            return foundMessage;
        while (response?.OdataNextLink != null)
        {
            var configuration = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri(response.OdataNextLink)
            };

            response = await graphClient.RequestAdapter.SendAsync(configuration, _ => new ChatMessageCollectionResponse(), cancellationToken: token);
            if (response?.Value == null) throw new NullReferenceException("Messages should not be null if there is a next page");
            foundMessage = response.Value.Select(s => s.GetCardThatHas(jsonFileName, uniqueId)).FirstOrDefault(x => x != null);
        }

        return foundMessage;
    }


    public async Task UploadFile(string teamId, string channelId, string fileLocation, Stream fileStream, CancellationToken token)
    {
        var filesFolder = await graphClient.Teams[teamId].Channels[channelId].FilesFolder.GetAsync(cancellationToken: token);
        var driveId = filesFolder?.ParentReference?.DriveId;
        var item = graphClient.Drives[driveId].Items["root"];
        // same as the list, we need to make sure you don't just drop it in the sharepoint site folder
        var content = item.ItemWithPath(fileLocation).Content;
        await content.PutAsync(fileStream, cancellationToken: token);
    }

    public async Task<string> GetFileUrl(string teamId, string channelId, string fileLocation, CancellationToken token)
    {
        var filesFolder = await graphClient.Teams[teamId].Channels[channelId].FilesFolder.GetAsync(cancellationToken: token);
        var driveId = filesFolder?.ParentReference?.DriveId;
        if (driveId == null) return string.Empty;
        var item = await GetDriveItem(driveId, fileLocation, token);
        if (item is { WebUrl: not null })
            // add web=1 to open in web view, this will make it possible to edit it in browser
            return item.WebUrl + "?web=1";
        return string.Empty;
    }
    public async Task<string> GetFileNameAsync(string teamId, string channelId, string fileLocation, CancellationToken token)
    {
        var filesFolder = await graphClient.Teams[teamId].Channels[channelId].FilesFolder.GetAsync(cancellationToken: token);
        var driveId = filesFolder?.ParentReference?.DriveId;
        if (driveId == null) return string.Empty;
        var item = await GetDriveItem(driveId, fileLocation, token);
        return item?.Name ?? string.Empty;
    }

    private async Task<DriveItem?> GetDriveItem(string driveId, string fileUrl, CancellationToken cancellationToken = default)
    {
        var path = Path.GetDirectoryName(fileUrl);
        var rootRequest = graphClient.Drives[driveId].Root;
        var children = rootRequest.ItemWithPath(path).Children;
        var driveItems = (await children.GetAsync(cancellationToken: cancellationToken))?.Value;
        return driveItems?.FirstOrDefault(x => x.Name == Path.GetFileName(fileUrl));
    }
  
}