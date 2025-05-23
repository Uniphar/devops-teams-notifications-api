namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamIdAsync(string teamName);
    Task<string> GetChannelIdAsync(string teamId, string channelName);
    Task<string?> GetMessageId(string teamId, string channelId, FileErrorModelOld modelToFind);
    Task<string?> GetMessageIdByUniqueId(string teamId, string channelId, string uniqueId);
    Task<string> UploadFile(string teamId, string channelId, string fileUrl, Stream fileStream);
    Task<string> GetFileUrl(string teamId, string channelId, string fileErrorFileName);
}