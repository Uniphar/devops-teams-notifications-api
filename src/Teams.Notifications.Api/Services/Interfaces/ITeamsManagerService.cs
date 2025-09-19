namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamIdAsync(string teamName, CancellationToken token);
    Task<string> GetChannelIdAsync(string teamId, string channelName, CancellationToken token);
    Task<string?> GetMessageIdByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId, CancellationToken token);
    Task<string> UploadFile(string teamId, string channelId, string fileUrl, Stream fileStream, CancellationToken token);
    Task CheckBotIsInTeam(string teamId, CancellationToken token);
    Task<string> GetGroupNameUniqueName(string groupId, CancellationToken token);
    Task<string> GetTeamName(string teamId, CancellationToken token);
    Task<string> GetFileNameAsync(string teamId, string channelId, string fileLocation, CancellationToken token);
    Task<ChatMessage?> GetMessageByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId, CancellationToken token);
}