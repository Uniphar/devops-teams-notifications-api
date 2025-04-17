using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFileErrorManagerService
{
    Task<string> CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModel fileError, string teamChannelId);
    Task DeleteFileErrorCard(string id, string teamChannelId);
}