using Azure.Core;

namespace Teams.Cards.BotFramework;

internal sealed class TokenCredentialHandler(TokenCredential Credential, params string[] Scopes) : DelegatingHandler()
{
	private TokenRequestContext TokenRequestContext { get; } = new TokenRequestContext(Scopes);

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = await Credential.GetTokenAsync(TokenRequestContext, cancellationToken);
		request.Headers.Add("Authorization", $"Bearer {token.Token}");
		return await base.SendAsync(request, cancellationToken);
	}
}
