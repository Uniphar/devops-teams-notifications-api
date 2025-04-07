using System.Runtime.CompilerServices;

namespace Teams.Notifications.Api.Util;

internal static class ContainerExtensions
{
	private static StringCache QueryCache { get; } = new StringCache();

	public static async Task<T?> GetByIdAsync<T>(this Container container, string id, PartitionKey partitionKey, CancellationToken cancellationToken = default(CancellationToken))
	{
		var result = await container.ReadItemAsync<T>(id, partitionKey, cancellationToken: cancellationToken);
		return result.StatusCode is System.Net.HttpStatusCode.OK
			? result.Resource
			: default;
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, string queryText, QueryRequestOptions options, CancellationToken cancellationToken, params ReadOnlySpan<(string name, object? value)> parameters)
	{
		var query = new QueryDefinition(queryText).WithParameters(parameters);
		return container.GetItemQueryIterator<T>(query, continuationToken: null, options).AsAsyncEnumerable(cancellationToken);
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, string queryText, CancellationToken cancellationToken, params ReadOnlySpan<(string name, object? value)> parameters)
	{
		var query = new QueryDefinition(queryText).WithParameters(parameters);
		return container.GetItemQueryIterator<T>(query, continuationToken: null, requestOptions: null).AsAsyncEnumerable(cancellationToken);
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, string queryText, params ReadOnlySpan<(string name, object? value)> parameters)
	{
		var query = new QueryDefinition(queryText).WithParameters(parameters);
		return container.GetItemQueryIterator<T>(query, continuationToken: null, requestOptions: null).AsAsyncEnumerable(default);
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, QueryRequestOptions options, CancellationToken cancellationToken, params ReadOnlySpan<(string prop, object? value)> properties)
	{
		using var queryBuilder = new ValueStringBuilder(stackalloc char[255]);
		queryBuilder.Append("SELECT * FROM Root WHERE 1=1");

		for (var i = 0; i < properties.Length; i++)
			queryBuilder.Append($" AND [{properties[i].prop}] = @param{i}");

		var query = new QueryDefinition(queryBuilder.ToString(QueryCache));
		for (var i = 0; i < properties.Length; i++)
			query.WithParameter(i, properties[i].value);

		return container.GetItemQueryIterator<T>(query, continuationToken: null, options).AsAsyncEnumerable(cancellationToken);
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, CancellationToken cancellationToken, params ReadOnlySpan<(string prop, object? value)> properties)
	{
		using var queryBuilder = new ValueStringBuilder(stackalloc char[255]);
		queryBuilder.Append("SELECT * FROM Root WHERE 1=1");

		for (var i = 0; i < properties.Length; i++)
			queryBuilder.Append($" AND [{properties[i].prop}] = @param{i}");

		var query = new QueryDefinition(queryBuilder.ToString(QueryCache));
		for (var i = 0; i < properties.Length; i++)
			query.WithParameter(i, properties[i].value);

		return container.GetItemQueryIterator<T>(query, continuationToken: null, requestOptions: null).AsAsyncEnumerable(cancellationToken);
	}

	public static IAsyncEnumerable<T> QueryItems<T>(this Container container, params ReadOnlySpan<(string prop, object? value)> properties)
	{
		using var queryBuilder = new ValueStringBuilder(stackalloc char[255]);
		queryBuilder.Append("SELECT * FROM Root WHERE 1=1");

		var paramIndex = 0;
		foreach (var (name, _) in properties)
			queryBuilder.Append($" AND [{name}] = @param{paramIndex++}");

		var query = new QueryDefinition(queryBuilder.ToString());
		paramIndex = 0;
		foreach (var (_, value) in properties)
			query.WithParameter($"@param{paramIndex++}", value);

		return container.GetItemQueryIterator<T>(query, continuationToken: null, requestOptions: null).AsAsyncEnumerable(default);
	}

	public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this FeedIterator<T> iterator, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		using (iterator)
			while(iterator.HasMoreResults)
				foreach (var item in await iterator.ReadNextAsync(cancellationToken))
					yield return item;
	}

	private static string[] IntParameters { get; } = ["@param0", "@param1", "@param2", "@param3", "@param4", "@param5", "@param6", "@param7", "@param8", "@param9", "@param10"];

	private static QueryDefinition WithParameter(this QueryDefinition query, int i, object? value)
	{
		if (i < IntParameters.Length)
			return query.WithParameter(IntParameters[i], value);
		else
			return query.WithParameter($"@param{i}", value);
	}

	private static QueryDefinition WithParameters(this QueryDefinition query, params ReadOnlySpan<(string name, object? value)> parameters)
	{
		foreach (var (name, value) in parameters)
			query.WithParameter(name, value);

		return query;
	}
}
