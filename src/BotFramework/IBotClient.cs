using Azure.Core;
using Refit;
using Teams.Cards.BotFramework.Serialization;

namespace Teams.Cards.BotFramework;

public interface IBotClient
{
	[Post("/v3/conversations/{conversationId}/activities")]
	Task<Id<string>> SendToConversation(string conversationId, [Body] Activity activity);

	[Put("/v3/conversations/{conversationId}/activities/{activityId}")]
	Task<Id<string>> UpdateActivity(string conversationId, string activityId, [Body] Activity activity);
}

public sealed class BotClientFactory
{
	private CachingFactory<Uri, IBotClient> Clients { get; }

	public BotClientFactory(TokenCredential credential)
	{
		var messageHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) }
			.AddTokenCredential(credential, "https://api.botframework.com/.default")
			.AddResiliency(builder => builder.AddDefaultRetry());

		var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
			IgnoreReadOnlyProperties = true
		});

		Clients = new CachingFactory<Uri, IBotClient>(baseUri =>
		{
			var httpClient = new HttpClient(messageHandler)
			{
				BaseAddress = baseUri
			};

			return RestService.For<IBotClient>(httpClient, new RefitSettings
			{
				ContentSerializer = contentSerializer
			});
		});

		DefaultClient = Clients.Get(new Uri("https://smba.trafficmanager.net/teams/"));
	}

	public IBotClient GetClientForServiceUri(Uri serviceUri) => Clients.Get(serviceUri);

	public IBotClient DefaultClient { get; }
}