using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamIdAsync(string teamName);
    Task<string> GetChannelIdAsync(string teamId, string channelName);
    Task<string?> GetMessageId(string teamId, string channelId, FileErrorModel modelToFind);
}