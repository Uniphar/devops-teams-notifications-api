namespace Teams.Notifications.Api.Services;

public class FrontgateApiService : IFrontgateApiService
{
    private readonly HttpClient _httpClient;

    public FrontgateApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, Stream fileStream, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        form.Add(streamContent, "file", fileName);
        return await _httpClient.PostAsync(uploadUrl, form);
    }
}

public interface IFrontgateApiService
{
    Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, Stream fileStream, string fileName);
}