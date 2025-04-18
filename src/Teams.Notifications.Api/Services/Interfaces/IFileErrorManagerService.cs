using System.IO;
using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFileErrorManagerService
{
    Task CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModel fileError, string teamId, string channelId, Stream? file = null);
}