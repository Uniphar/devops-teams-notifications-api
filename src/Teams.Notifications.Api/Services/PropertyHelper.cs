namespace Teams.Notifications.Api.Services;

public static class PropertyHelper
{
    public static string FindPropAndReplace<T>(this string content, T model, string property, string type, string fileUrl)
    {
        var toReplace = "{{" + property + ":" + type + "}}";
        return type switch
        {
            "string" => content.Replace(toReplace, model.TryGetStringPropertyValue(property) ?? string.Empty),
            "int" => content.Replace(toReplace, model.TryGetIntPropertyValue(property)?.ToString() ?? string.Empty),
            "file" => content.ReplaceForFile(toReplace, model, fileUrl),
            _ => content
        };
    }

    private static string ReplaceForFile<T>(this string content, string ToReplace, T model, string fileUrl)
    {
        var toReplaceWith = string.Empty;
        var file = model.GetFileValue();
        if (ToReplace == "{{FileUrl:file}}") toReplaceWith = fileUrl;
        if (file != null && ToReplace == "{{FileName:file}}") toReplaceWith = file.Name;
        content = content.Replace(ToReplace, toReplaceWith);
        return content;
    }

    private static int? TryGetIntPropertyValue<T>(this T model, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(int))
            return null;
        return (int?)(property.GetValue(model) ?? null);
    }

    private static string? TryGetStringPropertyValue<T>(this T model, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(string))
            return null;

        return property.GetValue(model) as string;
    }

    public static IFormFile? GetFileValue<T>(this T model)
    {
        var property = typeof(T).GetProperty("File", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(IFormFile))
            return null;

        return property.GetValue(model) as IFormFile;
    }
}