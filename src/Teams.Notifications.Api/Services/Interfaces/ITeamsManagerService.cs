using System.Threading.Tasks;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamId(string teamName);
    Task<string> GetChannelId(string teamId, string channelName);
}