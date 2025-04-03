namespace Graph.Enumerators.Generator;

internal static class ReflectionExtensions
{
	private static Dictionary<string, string> PrimitiveTypes { get; } = new()
	{
		{ "System.Byte", "byte" },
		{ "System.SByte", "sbyte" },
		{ "System.UInt16", "ushort" },
		{ "System.Int16", "short" },
		{ "System.UInt32", "uint" },
		{ "System.Int32", "int" },
		{ "System.UInt64", "ulong" },
		{ "System.Int64", "long" },
		{ "System.Single", "float" },
		{ "System.Double", "double" },
		{ "System.Decimal", "decimal" },
		{ "System.Char", "char" },
		{ "System.Boolean", "bool" },
	};

	private static HashSet<string?> UsingNamespaces { get; } = new([
		"System",
		"System.Threading.Tasks",
		"Microsoft.Graph",
		"Microsoft.Graph.Models",
		"Microsoft.Kiota.Abstractions",
		"Microsoft.Kiota.Abstractions.Serialization"
	]);

	public static string ToFullyQualifiedName(this Type type)
	{
		if (PrimitiveTypes.TryGetValue(type.FullName, out var primitiveName))
			return primitiveName;

		return type switch
		{
			{ IsGenericTypeDefinition: true } when UsingNamespaces.Contains(type.Namespace) => type.Name[0..^2],
			{ IsGenericType: true } when type.GetGenericTypeDefinition()?.FullName == "System.Nullable`1" => $"{Nullable.GetUnderlyingType(type)!.ToFullyQualifiedName()}?",

			{ IsNested: true, IsGenericTypeDefinition: true } => $"{type.DeclaringType!.ToFullyQualifiedName()}.{type.Name[0..^2]}",

			{ IsGenericTypeDefinition: true } when UsingNamespaces.Contains(type.Namespace) => $"{type.Name[0..^2]}",
			{ IsGenericTypeDefinition: true } => $"global::{type.Namespace}.{type.Name[0..^2]}",

			{ IsGenericType: true } => $"{type.GetGenericTypeDefinition().ToFullyQualifiedName()}<{string.Join(", ", type.GetGenericArguments().Select(ToFullyQualifiedName))}>",

			{ IsNested: true } => $"{type.DeclaringType!.ToFullyQualifiedName()}.{type.Name}",
			_ when UsingNamespaces.Contains(type.Namespace) => type.Name,
			_ => $"global::{type.Namespace}.{type.Name}"
		};
	}
}
