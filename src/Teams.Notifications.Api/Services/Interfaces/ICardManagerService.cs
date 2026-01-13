namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardManagerService
{
    Task CreateOrUpdateAsync<T>(string jsonFileName, IFormFile? formFile, T model, string teamName, string channelName, CancellationToken token) where T : BaseTemplateModel;
    Task DeleteCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token);
    Task<string?> GetCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token);
    Task CreateOrUpdateAsync<T>(string jsonFileName, T model, string user, CancellationToken token) where T : BaseTemplateModel;
    Task CreateMessageToUserAsync(string message, string user, CancellationToken cancellationToken);
    Task UpdateCardRemoveActionsAsync(ITurnContext turnContext, string[] actionsToRemove, CancellationToken cancellationToken);
}