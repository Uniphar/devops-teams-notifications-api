using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Teams.Notifications.Api.Services;

public class FrontgateApiService(IHttpClientFactory factory, IConfiguration configuration) : IFrontgateApiService
{
    private readonly HttpClient _httpClient = factory.CreateClient(Consts.FrontgateApiClient);

    private readonly TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        TenantId = Environment.GetEnvironmentVariable(Consts.ExternalTenantIdEnvironmentVariableName)
    });

    private readonly string frontgateApiScope = $"api://{Consts.FrontgateApiClient}/{configuration[Consts.FrontgateApiClientId]}/.default";


    public async Task<HttpResponseMessage> UploadFileAsync(string originalBlobUrl, LogicAppFrontgateFileInformation fileInfo)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext([frontgateApiScope]), CancellationToken.None);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await _httpClient.PostAsJsonAsync("/frontgate/Reprocess/reprocess-file-logic-app?originalBlobUrl=" + originalBlobUrl, fileInfo);
    }
}