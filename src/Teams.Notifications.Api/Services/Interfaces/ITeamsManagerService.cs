namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamIdAsync(string teamName);
    Task<string> GetChannelIdAsync(string teamId, string channelName);
    Task<string?> GetMessageIdByUniqueId(string teamId, string channelId, string jsonFileName, string uniqueId);
    Task<string> UploadFile(string teamId, string channelId, string fileUrl, Stream fileStream);
    Task CheckBotIsInTeam(string teamId);
    Task<string> GetGroupNameUniqueName(string teamId);
    Task<string> GetTeamName(string teamId);
    Task<string> GetFileNameAsync(string teamId, string channelId, string fileLocation);
}