namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardManagerService
{
    Task CreateCard(string jsonFileName, CardTemplateModel model, string teamId, string channelId);
    Task UpdateCard(string jsonFileName, string uniqueId, CardTemplateModel model, string teamId, string channelId);
    Task DeleteCard(string jsonFileName, string uniqueId, string teamId, string channelId);
}