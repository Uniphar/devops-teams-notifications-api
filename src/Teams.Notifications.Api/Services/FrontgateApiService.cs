using System.Net.Http.Headers;

namespace Teams.Notifications.Api.Services;

public class FrontgateApiService(IHttpClientFactory factory, IConfiguration configuration) : IFrontgateApiService
{
    private readonly HttpClient _httpClient = factory.CreateClient(Consts.FrontgateApiClient);

    private readonly TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        TenantId = Environment.GetEnvironmentVariable(Consts.ExternalTenantIdEnvironmentVariableName)
    });

    private readonly string frontgateApiScope = $"api://{Consts.FrontgateApiClient}/{configuration[Consts.FrontgateApiClientId]}/.default";


    public async Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, Stream fileStream, string fileName)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext([frontgateApiScope]), CancellationToken.None);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        form.Add(streamContent, "file", fileName);
        return await _httpClient.PostAsync(uploadUrl, form);
    }
}