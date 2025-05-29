using Microsoft.Kiota.Abstractions;

namespace Teams.Notifications.Api.Services;

public class TeamsManagerService(GraphServiceClient graphClient, IConfiguration config) : ITeamsManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(config), "Missing AZURE_CLIENT_ID");

    public async Task<string> GetTeamIdAsync(string teamName)
    {
        var groups = await graphClient.Teams.GetAsync(request =>
        {
            request.QueryParameters.Filter = $"displayName eq '{teamName}'";
            request.QueryParameters.Select = ["id"];
        });

        if (groups is not { Value: [Team { Id: var teamId }] })
            throw new InvalidOperationException($"Teams with displayName {teamName} does not exist");
        return teamId ?? throw new InvalidOperationException();
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
            throw new InvalidOperationException($"Channel with displayName {channelName} does not exist");
        return channelId ?? throw new InvalidOperationException();
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
            if (response?.Value == null) throw new InvalidOperationException("Messages should not be null if there is a next page");
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
        return (await file.GetAsync())?.WebUrl ?? string.Empty;
    }

    public async Task<string> GetFileUrl(string teamId, string channelId, string fileUrl) => (await (await GetFile(teamId, channelId, fileUrl)).GetAsync())?.WebUrl ?? string.Empty;

    private async Task<CustomDriveItemItemRequestBuilder> GetFile(string teamId, string channelId, string fileUrl)
    {
        var filesFolder = await graphClient.Teams[teamId].Channels[channelId].FilesFolder.GetAsync();
        var driveId = filesFolder?.ParentReference?.DriveId;

        var item = graphClient.Drives[driveId].Items["root"];
        var file = item.ItemWithPath(fileUrl);
        return file;
    }
}