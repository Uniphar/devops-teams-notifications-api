using System.Threading.Tasks;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Services
{
    public class FileErrorManagerService : IFileErrorManagerService
    {
        public FileErrorManagerService(ICardStatesService  cardStatesService) { }
        public async Task CreateFileErrorCard(FileErrorModel fileError) => throw new System.NotImplementedException();

        public async Task UpdateFileErrorCard(int id, FileErrorModel fileError) => throw new System.NotImplementedException();

        public async Task DeleteFileErrorCard(string id) => throw new System.NotImplementedException();
    }
}
