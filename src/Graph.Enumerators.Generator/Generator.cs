using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Graph.Enumerators.Generator;

[Generator]
public sealed class GraphEnumerateAsyncGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		/*var extensionMethods = context.MetadataReferencesProvider
			.Select((r, cancellationToken) =>
			{
				return r is PortableExecutableReference peRef
					? (name: Path.GetFileNameWithoutExtension(peRef.FilePath!), path: peRef.FilePath!)
					: default!;
			})
			.Where(r => r.name is not null && r.path is not null)
			.Collect()
			.SelectMany((refs, _) =>
			{
				var metadataContext = new MetadataLoadContext(new MetadataReferencesResolver(refs));
				var graphAssembly = metadataContext.LoadFromAssemblyName("Microsoft.Graph.Beta")
					?? metadataContext.LoadFromAssemblyName("Microsoft.Graph");

				if (graphAssembly is null)
					return [];

				var kiotaAbstractions = metadataContext.LoadFromAssemblyName("Microsoft.Kiota.Abstractions");

				if (kiotaAbstractions is null)
					return [];

				return ExtractBuilderTypes(graphAssembly, kiotaAbstractions);
			})
			.Select((types, _) => GenerateBuilderHelper(types))
			.Collect();*/

		var extensionMethods = ExtractBuilderTypes().Select(GenerateBuilderHelper).ToImmutableArray();
		var wrapperClass = GenerateWrappingClass(extensionMethods);

		context.RegisterPostInitializationOutput(context =>
		{
			context.AddSource("GraphEnumerateExtensions.g.cs", SourceText.From(typeof(GraphEnumerateAsyncGenerator).Assembly.GetManifestResourceStream("GraphEnumerateExtensions.cs"), canBeEmbedded: true));
			context.AddSource("GraphEnumerateExtensions.methods.g.cs", SourceText.From(wrapperClass));
		});
	}

	private static IEnumerable<BuilderType> ExtractBuilderTypes(/*Assembly graphAssembly, Assembly kiotaAbstractions*/)
	{
		var baseRequestBuilder = typeof(BaseRequestBuilder);
		var baseCollectionPaginationCountResponse = typeof(BaseCollectionPaginationCountResponse);

		return typeof(GraphServiceClient).Assembly.GetTypes()
			.Where(t => t.Name.EndsWith("RequestBuilder"))
			.Where(t => baseRequestBuilder.IsAssignableFrom(t))
			.Select(builderType =>
			{
				var getMethod = builderType.GetMethod("GetAsync");

				if (getMethod is null)
					return null;

				var requestConfigurationType = getMethod.GetParameters()[0].ParameterType.GetGenericArguments()[0];
				var collectionType = getMethod.ReturnType.GetGenericArguments()[0];

				if (!baseCollectionPaginationCountResponse.IsAssignableFrom(collectionType))
					return null;

				var entityType = collectionType.GetProperty("Value")!.PropertyType.GetGenericArguments()[0];

				return new BuilderType
				{
					Builder = builderType.ToFullyQualifiedName(),
					RequestConfiguration = requestConfigurationType.ToFullyQualifiedName(),
					Collection = collectionType.ToFullyQualifiedName(),
					Entity = entityType.ToFullyQualifiedName()
				};
			})
			.Where(types => types is not null)
			.Select(types => types!);
	}

	private static string GenerateBuilderHelper(BuilderType types)
	{
		return @$"
public static IAsyncEnumerable<{types.Entity}> EnumerateAsync(this {types.Builder} builder, Action<{types.RequestConfiguration}> requestConfiguration = null, RequestConfigurator? requestConfigurator = null, ErrorMapping? errorMapping = null, CancellationToken cancellationToken = default)
{{
	return EnumerateAsync<{types.Entity}, {types.Collection}>(
		builder: builder,
		firstResponse: builder.GetAsync(requestConfiguration, cancellationToken),
		getEntities: static collection => collection.Value
	);
}}";
	}

	private static string GenerateWrappingClass(ImmutableArray<string> methods)
	{
		return $@"
		using System;
		using System.Runtime.CompilerServices;
		using System.Threading.Tasks;
	
		using Microsoft.Graph;
		using Microsoft.Graph.Models;
	
		using Microsoft.Kiota.Abstractions;
		using Microsoft.Kiota.Abstractions.Serialization;
	
		using RequestConfigurator = Func<RequestInformation, RequestInformation>;
		using ErrorMapping = Dictionary<string, ParsableFactory<IParsable>>;
	
		namespace Microsoft.Graph;
	
		internal static partial class GraphEnumerateExtensions
		{{	
			{string.Join("\n\n", methods)}
		}}
		";
	}
}

internal sealed record BuilderType
{
	public required string Builder { get; init; }
	public required string RequestConfiguration { get; init; }
	public required string Collection { get; init; }
	public required string Entity { get; init; }
}

/*internal sealed class MetadataReferencesResolver(ImmutableArray<(string name, string path)> References) : MetadataAssemblyResolver
{
	public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
	{
		return References.FirstOrDefault(r => r.name == assemblyName.Name) is (string _, string path)
			? context.LoadFromAssemblyPath(path)
			: null;
	}
}*/