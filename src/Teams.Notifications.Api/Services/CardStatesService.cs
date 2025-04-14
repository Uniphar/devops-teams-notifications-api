using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services;

public class CardStatesService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITeamsChannelManagingService _managingService;

    public CardStatesService(IMemoryCache memoryCache, ITeamsChannelManagingService managingService)
    {
        _memoryCache = memoryCache;
        _managingService = managingService;
    }

    public async Task<CardState> GetOrUpdate(CardState currentState)
    {
        var key = GetKeyFromFileError(currentState.FileError);
        if (!_memoryCache.TryGetValue(key, out CardState? cacheValue)) cacheValue = currentState;

        if (cacheValue == null) throw new NullReferenceException(nameof(cacheValue));
        if (string.IsNullOrEmpty(cacheValue.TeamId) || string.IsNullOrEmpty(cacheValue.ChannelId))
        {
            var keys = await _managingService.GetTeamAndChannelId(cacheValue.TeamName, cacheValue.ChannelName);
            cacheValue.TeamId = keys.Key;
            cacheValue.ChannelId = keys.Value;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions();


        _memoryCache.Set(key, cacheValue, cacheEntryOptions);
        return cacheValue;
    }

    private static string GetKeyFromFileError(FileErrorModel currentStateFileError) => currentStateFileError.System + currentStateFileError.JobId + currentStateFileError.FileName;
}