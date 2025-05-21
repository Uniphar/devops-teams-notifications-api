using System.Reflection;

namespace Teams.Notifications.Api.Services;

public static class StringHelper
{
    public static string FindPropAndReplace<T>(this string content, T model, string property, string type)
    {
        var toReplace = "{{" + property + ":" + type + "}}";
        return type switch
        {
            "string" => content.Replace(toReplace, TryGetStringPropertyValue(model, property)),
            "int" => content.Replace(toReplace, TryGetIntPropertyValue(model, property).GetValueOrDefault().ToString()),
            _ => content
        };
    }

    private static int? TryGetIntPropertyValue<T>(T model, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(int))
            return null;
        return (int?)(property.GetValue(model) ?? null);
    }

    private static string? TryGetStringPropertyValue<T>(T model, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(string))
            return null;

        return property.GetValue(model) as string;
    }
}