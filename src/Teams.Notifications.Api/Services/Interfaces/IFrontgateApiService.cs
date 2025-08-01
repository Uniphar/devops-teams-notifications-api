namespace Teams.Notifications.Api.Services.Interfaces;

public interface IFrontgateApiService
{
    Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, Stream fileStream, string fileName);
}