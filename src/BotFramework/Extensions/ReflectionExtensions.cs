using System.Linq.Expressions;

namespace Teams.Cards.BotFramework;

internal static class ReflectionExtensions
{
	public static bool IsImmutableArray(this Type? type)
		=> type is { IsValueType: true, IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>);

	private static ImmutableArray<Type> ValueTupleTypes = [
		typeof(ValueTuple<,>),
		typeof(ValueTuple<,,>),
		typeof(ValueTuple<,,,>),
		typeof(ValueTuple<,,,,>),
		typeof(ValueTuple<,,,,,>),
		typeof(ValueTuple<,,,,,,>),
		typeof(ValueTuple<,,,,,,,>)
	];

	public static bool IsValueTuple(this Type? type)
		=> type is { IsValueType: true, IsGenericType: true } && ValueTupleTypes.Contains(type.GetGenericTypeDefinition());

	public static bool IsGenericOfType(this Type type, Type openGenericType)
	{
		if (!openGenericType.IsGenericTypeDefinition)
			throw new ArgumentException("Must be an open generic type", nameof(openGenericType));

		return type.IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
	}

	public static bool HasInterface(this Type type, Type interfaceType)
	{
		if (!interfaceType.IsInterface)
			throw new ArgumentException("Must be an interface type", nameof(interfaceType));

		return interfaceType.IsGenericTypeDefinition
			? type.GetInterfaces().Any(i => i.IsGenericOfType(interfaceType))
			: type.GetInterfaces().Any(i => i == interfaceType);
	}

	public static IEnumerable<Type> GetInterfacesOfType(this Type type, Type interfaceType)
	{
		if (!interfaceType.IsInterface)
			throw new ArgumentException("Must be an interface type", nameof(interfaceType));

		return interfaceType.IsGenericTypeDefinition
			? type.GetInterfaces().Where(i => i.IsGenericOfType(interfaceType))
			: type.GetInterfaces().Where(i => i == interfaceType);
	}

	public static T InstantiateGeneric<T>(this Type type, params Type[] genericArgs)
		=> (T)Activator.CreateInstance(type.MakeGenericType(genericArgs))!;

	public static T Instantiate<T>(this Type type, params object[] arguments)
	{
		if (!type.IsAssignableTo(typeof(T)))
			throw new ArgumentException($"Type `{type.FullName}` is not compatible with type `{typeof(T).FullName}`");

		return (T)Activator.CreateInstance(type, arguments)!;
	}

	public static Delegate CreateDelegate(this ConstructorInfo constructor)
	{
		var parameters = constructor.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
		var construct = Expression.New(constructor, parameters);
		return Expression.Lambda(construct, parameters).Compile();
	}

	public static IEnumerable<(MethodInfo method, Type from, Type to)> GetImplicitCasts(this Type type)
	{
		return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.IsSpecialName && m.Name == "op_Implicit")
			.Select(m => (m, m.GetParameters()[0].ParameterType, m.ReturnType));
	}

	public static MethodInfo? GetImplicitCast(this Type type, Type from, Type to)
	{
		return type.GetImplicitCasts().FirstOrDefault(cast => cast.from == from && cast.to == to).method;
	}
}