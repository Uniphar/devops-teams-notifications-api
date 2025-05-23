namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFileErrorManagerService
{
    Task CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModelOld fileError, string teamId, string channelId);
}