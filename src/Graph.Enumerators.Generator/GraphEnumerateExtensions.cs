using System.Runtime.CompilerServices;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

using RequestConfigurator = System.Func<Microsoft.Kiota.Abstractions.RequestInformation, Microsoft.Kiota.Abstractions.RequestInformation>;
using ErrorMapping = System.Collections.Generic.Dictionary<string, Microsoft.Kiota.Abstractions.Serialization.ParsableFactory<Microsoft.Kiota.Abstractions.Serialization.IParsable>>;

namespace Microsoft.Graph;

internal static partial class GraphEnumerateExtensions
{
	private static Dictionary<string, ParsableFactory<IParsable>> DefaultErrorMapping = new(StringComparer.OrdinalIgnoreCase)
	{
		{"XXX", parsable => new ServiceException("Error while enumerating items, see inner exception for details", new Exception(parsable.GetErrorMessage())) }
	};

	private static string GetErrorMessage(this IParseNode responseParseNode)
	{
		var errorParseNode = responseParseNode.GetChildNode("error");
		var errorCode = errorParseNode?.GetChildNode("code")?.GetStringValue();
		var errorMessage = errorParseNode?.GetChildNode("message")?.GetStringValue();
		// concatenate the error code and message
		return $"{errorCode} : {errorMessage}";
	}
				
	private static Task<TCollection?> GetNextPage<TCollection>(this IRequestAdapter adapter, string nextLink, RequestConfigurator? requestConfigurator, ErrorMapping? errorMapping, CancellationToken cancellationToken)
		where TCollection : IParsable, new()
	{
		var nextPageRequest = new RequestInformation(Method.GET, nextLink, null!);
		var configuredRequest = requestConfigurator is not null
			? requestConfigurator(nextPageRequest)
			: nextPageRequest;
		return adapter.SendAsync(configuredRequest, _ => new TCollection(), errorMapping ?? DefaultErrorMapping, cancellationToken);
	}


	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_RequestAdapter")]
	private static extern IRequestAdapter GetRequestAdapter(this BaseRequestBuilder builder);

	private static async IAsyncEnumerable<TEntity> EnumerateAsync<TEntity, TCollection>(
		this BaseRequestBuilder builder,
		Task<TCollection?> firstResponse,
		Func<TCollection, List<TEntity>?> getEntities,
		RequestConfigurator? requestConfigurator = null,
		ErrorMapping? errorMapping = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
		where TCollection : BaseCollectionPaginationCountResponse, new()
	{
		var nextResponse = firstResponse;
		while (nextResponse is not null)
		{

			var currentResponse = await nextResponse;

			if (currentResponse is null)
				yield break;

			nextResponse = currentResponse.OdataNextLink is string nextLink
				? builder.GetRequestAdapter().GetNextPage<TCollection>(nextLink, requestConfigurator, errorMapping, cancellationToken)
				: null;

			if (currentResponse is null || getEntities(currentResponse) is not List<TEntity> entities)
				continue;

			foreach (var item in entities)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return item;
			}
		}
	}
}