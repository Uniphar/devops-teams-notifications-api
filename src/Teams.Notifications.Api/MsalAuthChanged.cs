using System.Collections.Concurrent;
using System.Data;
using Microsoft.Agents.Authentication;
using Microsoft.Agents.Authentication.Msal;
using Microsoft.Identity.Client;
using Teams.Notifications.Api.Services;

namespace Teams.Notifications.Api;
/// <summary>
/// Taken from MSAL auth to do this: https://github.com/microsoft/Agents-for-net/pull/228
/// </summary>
public class MsalAuthChanged : IAccessTokenProvider, IMSALProvider
{
    private readonly ConcurrentDictionary<Uri, AuthResults> _cacheList = new();
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly string? _federatedTokenFile;
    private readonly string _tenantId;
    private string? _lastJwtWorkLoadIdentity;
    private DateTimeOffset _lastReadWorkloadIdentity;


    public MsalAuthChanged(IServiceProvider systemServiceProvider, IConfigurationSection msalConfigurationSection)
    {
        var config = systemServiceProvider.GetRequiredService<IConfiguration>();
        _clientId = config["AZURE_CLIENT_ID"] ?? throw new NoNullAllowedException("ClientId is required");
        _tenantId = config["AZURE_TENANT_ID"] ?? throw new NoNullAllowedException("TenantId is required");
        _clientSecret = config["ClientSecret"];
        _federatedTokenFile = config["AZURE_FEDERATED_TOKEN_FILE"];
    }


    public async Task<string> GetAccessTokenAsync(string resourceUrl, IList<string> scopes, bool forceRefresh = false)
    {
        if (!Uri.IsWellFormedUriString(resourceUrl, UriKind.RelativeOrAbsolute)) throw new ArgumentException("Invalid instance URL");

        Uri instanceUri = new(resourceUrl);
        var localScopes = new List<string> { "https://api.botframework.com/.default" }.ToArray();

        // Get or create existing token. 
        if (_cacheList.ContainsKey(instanceUri))
        {
            if (!forceRefresh)
            {
                var accessToken = _cacheList[instanceUri].MsalAuthResult.AccessToken;
                var tokenExpiresOn = _cacheList[instanceUri].MsalAuthResult.ExpiresOn;
                if (tokenExpiresOn != null && tokenExpiresOn < DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(30)))
                {
                    accessToken = string.Empty; // flush the access token if it is about to expire.
                    _cacheList.Remove(instanceUri, out _);
                }

                if (!string.IsNullOrEmpty(accessToken)) return accessToken;
            }
            else
                _cacheList.Remove(instanceUri, out _);
        }

        var msalAuthClient = InnerCreateClientApplication();

        if (localScopes.Length == 0) throw new ArgumentException("At least one Scope is required for Client Authentication.");

        var authResult = await msalAuthClient.AcquireTokenForClient(localScopes).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
        var authResultPayload = new AuthResults
        {
            MsalAuthResult = authResult,
            TargetServiceUrl = instanceUri,
            MsalAuthClient = msalAuthClient
        };

        if (_cacheList.ContainsKey(instanceUri))
            _cacheList[instanceUri] = authResultPayload;
        else
            _cacheList.TryAdd(instanceUri, authResultPayload);

        return authResultPayload.MsalAuthResult.AccessToken;
    }

    public IApplicationBase CreateClientApplication() => InnerCreateClientApplication();

    private IConfidentialClientApplication InnerCreateClientApplication()
    {
        // initialize the MSAL client
        var cAppBuilder = ConfidentialClientApplicationBuilder
            .CreateWithApplicationOptions(
                new ConfidentialClientApplicationOptions
                {
                    ClientId = _clientId
                })
            .WithLegacyCacheCompatibility(false)
            .WithCacheOptions(new CacheOptions(true));


        cAppBuilder.WithTenantId(_tenantId);

        if (!string.IsNullOrWhiteSpace(_clientSecret))
            cAppBuilder.WithClientSecret(_clientSecret);
        else
            cAppBuilder.WithClientAssertion(() =>
            {
                // read only once every 5 minutes, less heavy for I/O
                if (_lastJwtWorkLoadIdentity != null && DateTimeOffset.UtcNow.Subtract(_lastReadWorkloadIdentity) <= TimeSpan.FromMinutes(5))
                    return _lastJwtWorkLoadIdentity;
                _lastReadWorkloadIdentity = DateTimeOffset.UtcNow;
                _lastJwtWorkLoadIdentity = File.ReadAllText(_federatedTokenFile!);
                return _lastJwtWorkLoadIdentity;
            });

        return cAppBuilder.Build();
    }
}