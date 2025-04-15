using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ITeamsChannelMessagingService
{
    Task UpdateFileErrorCard(FileErrorModel model, string teamId, string channelId, string messageId);
    Task<string> CreateFileErrorCard(FileErrorModel model, string teamId, string channelId);
    Task DeleteFileErrorCard(string teamId, string channelId, string messageId);
}