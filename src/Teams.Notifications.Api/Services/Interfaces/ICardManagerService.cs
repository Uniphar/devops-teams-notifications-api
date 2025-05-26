namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardManagerService
{
    Task CreateOrUpdate<T>(string jsonFileName, T model, string teamName, string channelName) where T : BaseTemplateModel;
    Task DeleteCard(string jsonFileName, string uniqueId, string teamName, string channelName);
}