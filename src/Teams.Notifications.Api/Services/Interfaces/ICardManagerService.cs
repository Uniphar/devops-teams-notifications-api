﻿namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardManagerService
{
    Task CreateOrUpdateAsync<T>(string jsonFileName, IFormFile? formFile, T model, string teamName, string channelName, CancellationToken token) where T : BaseTemplateModel;
    Task DeleteCardAsync(string jsonFileName, string uniqueId, string teamName, string channelName, CancellationToken token);
}