using System.Collections.Concurrent;
using System.Data;
using Microsoft.Agents.Authentication;
using Microsoft.Agents.Authentication.Msal;
using Microsoft.Identity.Client;
using Teams.Notifications.Api.Services;

namespace Teams.Notifications.Api;

/// <summary>
///     Taken from MSAL auth to do this: https://github.com/microsoft/Agents-for-net/pull/228
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

// instead of using the config we can directly pull and set all the env variables, unlike the MsalAuth option
    public MsalAuthChanged(IServiceProvider systemServiceProvider, IConfigurationSection msalConfigurationSection)
    {
        var config = systemServiceProvider.GetRequiredService<IConfiguration>();
        _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(nameof(systemServiceProvider), "Missing AZURE_CLIENT_ID");
        _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(nameof(systemServiceProvider), "Missing AZURE_TENANT_ID");
        _clientSecret = config["ClientSecret"];
        _federatedTokenFile = config["AZURE_FEDERATED_TOKEN_FILE"];
        if (string.IsNullOrWhiteSpace(_clientSecret) && string.IsNullOrWhiteSpace(_federatedTokenFile)) throw new NoNullAllowedException("Secret or token file is required");
    }

    /// <summary>
    ///     simplified version of the MsalAuth from the sdk, just to ease our use case
    /// </summary>
    /// <param name="resourceUrl"></param>
    /// <param name="scopes"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
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
                if (tokenExpiresOn < DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(30)))
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
            MsalAuthResult = authResult
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
        // same as: src\libraries\Authentication\Authentication.Msal\MsalAuth.cs:118
        var cAppBuilder = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(
            new ConfidentialClientApplicationOptions
            {
                ClientId = _clientId
            });
        // we only use tenant so this is perfect
        cAppBuilder.WithTenantId(_tenantId);

        // we want to have an option for client secret for local dev
        if (!string.IsNullOrWhiteSpace(_clientSecret))
            cAppBuilder.WithClientSecret(_clientSecret);
        else
            // same as https://github.com/microsoft/Agents-for-net/pull/228/files#diff-44f4195f52226b6d9dc9b4c2dd14855625ebdfdca19c04baf53bd7ebe619939eR233
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