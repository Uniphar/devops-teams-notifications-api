using System.Collections.Generic;
using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsChannelManagingService
{
    Task<KeyValuePair<string, string>> GetTeamAndChannelId(string teamName, string channelName);
    Task UpdateFileErrorCard(FileErrorModel model,string teamId, string channelId, string messageId);
    Task<string> CreateFileErrorCard(FileErrorModel model, string teamId, string channelId);
    Task DeleteFileErrorCard(string teamId, string channelId, string messageId);
}