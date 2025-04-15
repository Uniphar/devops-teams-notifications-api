using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFileErrorManagerService
{
    Task CreateFileErrorCard(FileErrorModel fileError);
    Task UpdateFileErrorCard(int id, FileErrorModel fileError);
    Task DeleteFileErrorCard(string id);
}