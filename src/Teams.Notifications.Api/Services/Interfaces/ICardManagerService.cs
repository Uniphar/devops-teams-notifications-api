namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardManagerService
{
    Task CreateCard<T>(string jsonFileName, T model, string teamId, string channelId) where T : BaseTemplateModel;
    Task UpdateCard<T>(string jsonFileName, T model, string teamId, string channelId) where T : BaseTemplateModel;
    Task DeleteCard(string jsonFileName, string uniqueId, string teamId, string channelId);
}