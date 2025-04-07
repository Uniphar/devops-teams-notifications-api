using Azure.Core;

namespace Teams.Cards.Api;

internal static class CosmosServiceConfiguration
{
	private static bool InstantiationRegistered { get; set; }

	public static CosmosBuilder AddCosmos(this IServiceCollection services, string serviceKey, string endpoint)
		=> services.AddCosmos(serviceKey, endpoint, credential: null!);

	public static CosmosBuilder AddCosmos(this IServiceCollection services, string endpoint, TokenCredential credential)
		=> services.AddCosmos(endpoint, endpoint, credential);

	public static CosmosBuilder AddCosmos(this IServiceCollection services, string endpoint)
		=> services.AddCosmos(endpoint, endpoint, null!);

	public static CosmosBuilder AddCosmos(this IServiceCollection services, string? serviceKey, string endpoint, TokenCredential credential)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(serviceKey);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);

		if (!InstantiationRegistered)
		{
			services.AddHostedService(_ => CosmosContainerInstantiation.Instance);
			InstantiationRegistered = true;
		}

		services.AddKeyedSingleton(serviceKey, (serviceProvider, _) =>
		{
			var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
			var usedCredential = credential ?? serviceProvider.GetRequiredService<TokenCredential>();

			return new CosmosClient(endpoint, credential, new CosmosClientOptions()
			{
				HttpClientFactory = httpClientFactory.CreateClient,
				UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				}
			});
		});

		return new CosmosBuilder(services, serviceKey);
	}
}

public sealed class CosmosBuilder(IServiceCollection services, string serviceKey)
{
	public CosmosDatabaseBuilder AddDatabase(string key, string database, ThroughputProperties? throughputProperties = default, RequestOptions? requestOptions = default)
	{

	}

	public CosmosBuilder AddContainer(string key, string database, string container, string partitionKeyPath, int? throughput = default, RequestOptions? requestOptions = default)
	{
		services.AddKeyedSingleton(key, (serviceProvider, _) =>
		{
			var cosmosClient = serviceProvider.GetRequiredKeyedService<CosmosClient>(serviceKey);
			
			var cosmosDb = cosmosClient.GetDatabase(database);
			cosmosDb.CreateContainerIfNotExistsAsync()
			PendingTasks.Add(database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath, throughput, requestOptions));
			return database.GetContainer(containerId);
		});

		return this;
	}
}

public sealed class CosmosDatabaseBuilder(IServiceCollection service, string serviceKey)
{

}

file sealed class CosmosContainerInstantiation(IServiceProvider serviceProvider) : IHostedLifecycleService
{
	private static List<DatabaseDef> Databases { get; } = new();
	private static List<ContainerDef> Containers { get; } = new();

	public static void AddDatabase(string serviceKey, string databaseId, ThroughputProperties? throughputProperties, RequestOptions? requestOptions)
		=> Databases.Add(new DatabaseDef(serviceKey, databaseId, throughputProperties, requestOptions));

	public static void AddContainer(string databaseId, ContainerProperties properties, ThroughputProperties? throughputProperties, RequestOptions? requestOptions)
		=> Containers.Add(new ContainerDef(databaseId, properties, throughputProperties, requestOptions));

	async Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken)
	{
		foreach (var (clientServiceKey, databaseId, throughputProperties, requestOptions) in Databases)
		{
			var cosmosClient = serviceProvider.GetRequiredKeyedService<CosmosClient>(clientServiceKey);
			await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId, throughputProperties, requestOptions, cancellationToken);
		}

		foreach (var (databaseServiceKey, properties, throughputProperties, requestOptions) in Containers)
		{
			var database = serviceProvider.GetRequiredKeyedService<Database>(databaseServiceKey);
			await database.CreateContainerIfNotExistsAsync(properties, throughputProperties, requestOptions, cancellationToken);
		}
	}

	private record struct DatabaseDef(string ClientServiceKey, string DatabaseId, ThroughputProperties? ThroughputProperties, RequestOptions? RequestOptions);
	private record struct ContainerDef(string DatabaseServiceKey, ContainerProperties properties, ThroughputProperties? ThroughputProperties, RequestOptions? RequestOptions);
	
	Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}