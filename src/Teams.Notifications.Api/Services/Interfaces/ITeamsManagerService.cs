using System.Threading.Tasks;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsManagerService
{
    Task<string> GetTeamId(string teamName);
    Task<MinimalChannelInfo> GetChannelInfo(string teamId, string channelName);
}