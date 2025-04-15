using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services;

public class CardStatesService : ICardStatesService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITeamsChannelMessagingService _messagingService;
    private readonly ITeamsManagerService _teamsManager;

    public CardStatesService(IMemoryCache memoryCache, ITeamsChannelMessagingService messagingService, ITeamsManagerService teamsManager)
    {
        _memoryCache = memoryCache;
        _messagingService = messagingService;
        _teamsManager = teamsManager;
    }

    public async Task<CardState> GetOrUpdate(CardState currentState)
    {
        var key = currentState.FileError.GetHashCode();
        if (!_memoryCache.TryGetValue(key, out CardState? cacheValue)) cacheValue = currentState;

        if (cacheValue == null) throw new NullReferenceException(nameof(cacheValue));
        if (string.IsNullOrEmpty(cacheValue.TeamId) || string.IsNullOrEmpty(cacheValue.ChannelId))
        {
            var teamId = await _teamsManager.GetTeamId(cacheValue.TeamName);
            var channelId = await _teamsManager.GetChannelId(teamId, cacheValue.ChannelName);
            cacheValue.TeamId = teamId;
            cacheValue.ChannelId = channelId;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions();


        _memoryCache.Set(key, cacheValue, cacheEntryOptions);
        return cacheValue;
    }

   
}