namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFrontgateApiService
{
    Task<HttpResponseMessage> UploadFileAsync(string originalBlobUrl, LogicAppFrontgateFileInformation fileInfo, CancellationToken cancellationToken);
}