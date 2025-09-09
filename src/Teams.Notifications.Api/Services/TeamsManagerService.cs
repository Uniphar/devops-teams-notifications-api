namespace Teams.Notifications.Api.Services;

public class TeamsManagerService(GraphServiceClient graphClient, IConfiguration config) : ITeamsManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");

    public async Task CheckBotIsInTeam(string teamId)
    {
        var result = await graphClient
            .Teams[teamId]
            .InstalledApps
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Expand = ["teamsAppDefinition"];
                requestConfiguration.QueryParameters.Filter = $"teamsAppDefinition/authorization/clientAppId eq '{_clientId}'";
            });
        if (result?.Value?.Count == 0) throw new InvalidOperationException("Please install the bot on the Team, it is not installed at the moment");
    }

    public async Task<string> GetTeamIdAsync(string teamName)
    {
        var groups = await graphClient.Teams.GetAsync(request =>
        {
            request.QueryParameters.Filter = $"displayName eq '{teamName}'";
            request.QueryParameters.Select = ["id"];
        });

        if (groups is not { Value: [{ Id: var teamId }] })
            throw new InvalidOperationException($"Team with name {teamName} does not exist");
        return teamId ?? throw new InvalidOperationException($"Team with name {teamName} does not exist");
    }

    public async Task<string> GetChannelIdAsync(string teamId, string channelName)
    {
        var channels = await graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(request =>
            {
                request.QueryParameters.Filter = $"displayName eq '{channelName}'";
                request.QueryParameters.Select = ["id"];
            });

        if (channels is not { Value: [{ Id: var channelId }] })
            throw new InvalidOperationException($"Channel with name {channelName} does not exist");
        return channelId ?? throw new InvalidOperationException($"Channel with name {channelName} does not exist");
    }

    public async Task<string> GetChannelNameAsync(string teamId, string channelId)
    {
        var channel = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .GetAsync();
        if (channel != null && channel.Id != channelId)
            throw new InvalidOperationException($"Channel with id {channelId} does not exist");
        var channelName = channel?.DisplayName;
        return channelName ?? throw new InvalidOperationException($"Channel with id {channelId} does not exist");
    }

    public async Task<string> GetGroupNameUniqueName(string teamId)
    {
        var group = await graphClient
            .Teams[teamId]
            .Group
            .GetAsync();
        if (group == null)
            throw new InvalidOperationException($" No group found for team {teamId}");
        return group.UniqueName ?? throw new InvalidOperationException($"Team: {teamId} parent groups unique name could not be found");
    }

    public async Task<string> GetGroupName(string teamId)
    {
        var team = await graphClient
            .Teams[teamId]
            .GetAsync();
        if (team == null)
            throw new InvalidOperationException($" No team found for id {teamId}");
        return team.DisplayName ?? throw new InvalidOperationException("Display name is empty");
    }


    public async Task<string?> GetMessageIdByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId)
    {
        // we have to get the full thing since select or filter is not allowed, but we can request 100 messages at a time
        var response = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(x => { x.QueryParameters.Top = 100; });
        var responses = response
            ?.Value
            ?.Where(x => x.DeletedDateTime == null &&
                         x.From?.Application != null &&
                         x.From.Application.Id == _clientId
            )
            .ToList();
        // no need to do anything if there is no message
        if (responses == null) return null;
        var id = responses.FirstOrDefault(s => s.GetMessageThatHas(jsonFileName, uniqueId))?.Id;
        if (!string.IsNullOrWhiteSpace(id))
            return id;
        while (response?.OdataNextLink != null)
        {
            var configuration = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri(response.OdataNextLink)
            };

            response = await graphClient.RequestAdapter.SendAsync(configuration, _ => new ChatMessageCollectionResponse());
            if (response?.Value == null) throw new NullReferenceException("Messages should not be null if there is a next page");
            id = response.Value.FirstOrDefault(s => s.GetMessageThatHas(jsonFileName, uniqueId))?.Id;
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return id;
    }


    public async Task<string> UploadFile(string teamId, string channelId, string fileUrl, Stream fileStream)
    {
        var file = await GetFile(teamId, channelId, fileUrl);
        var content = file.Content;
        await content.PutAsync(fileStream);
        var fileFound = await file.GetAsync();
        if (fileFound is { WebUrl: not null })
            // add web=1 to open in web view, this will make it possible to edit it in browser
            return fileFound.WebUrl + "?web=1";
        return string.Empty;
    }

    public async Task<string> GetFileUrl(string teamId, string channelId, string fileUrl) => (await (await GetFile(teamId, channelId, fileUrl)).GetAsync())?.WebUrl ?? string.Empty;

    public async Task<string> GetFileNameAsync(string teamId, string channelId, string fileUrl) => (await (await GetFile(teamId, channelId, fileUrl)).GetAsync())?.Name ?? string.Empty;


    public async Task<KeyValuePair<string, Stream>> GetFileStreamAsync(string teamId, string channelId, string fileUrl)
    {
        var file = await GetFile(teamId, channelId, fileUrl);
        var fileMeta = await file.GetAsync();
        var content = await file.Content.GetAsync() ?? Stream.Null;
        var fileName = fileMeta?.Name ?? Path.GetFileName(fileUrl);

        return new KeyValuePair<string, Stream>(fileName, content);
    }

    private async Task<CustomDriveItemItemRequestBuilder> GetFile(string teamId, string channelId, string fileUrl)
    {
        var filesFolder = await graphClient.Teams[teamId].Channels[channelId].FilesFolder.GetAsync();
        var driveId = filesFolder?.ParentReference?.DriveId;

        var item = graphClient.Drives[driveId].Items["root"];
        var file = item.ItemWithPath(fileUrl);
        return file;
    }
}