using System.Reflection;

namespace Teams.Notifications.Api.Extensions;

public static class ReflectionExtensions
{
    public static bool HasInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(interfaceType);

        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{nameof(interfaceType)} must be an interface", nameof(interfaceType));

        return interfaceType.IsGenericTypeDefinition
            ? type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
            : type.GetInterfaces().Contains(interfaceType);
    }

    public static bool HasAttribute<T>(this ICustomAttributeProvider? provider, bool inherit = false) where T : Attribute => provider?.IsDefined(typeof(T), inherit) is true;

    public static bool IsNullableOrReferenceType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    public static MethodInfo GetMethod(Delegate del) => del.Method;
}