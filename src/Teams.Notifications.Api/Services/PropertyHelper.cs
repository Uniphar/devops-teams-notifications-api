namespace Teams.Notifications.Api.Services;

public static class PropertyHelper
{
    public static string FindPropAndReplace<T>(this string jsonString, T model, string property, string type, string fileUrl)
    {
        var toReplace = "{{" + property + ":" + type + "}}";
        switch (type)
        {
            // optional string, will remove the block if empty
            case "string?":
                var value = model.TryGetStringPropertyValue(property);
                if (string.IsNullOrEmpty(value))
                {
                    // Remove the entire object from the array where the placeholder is found
                    // This regex matches an object in an array containing the placeholder as a value
                    var pattern = $@"\{{[^{{}}]*?""[^""]+""\s*:\s*"".*?{Regex.Escape(toReplace)}.*?""[^{{}}]*?\}}";
                    jsonString = Regex.Replace(jsonString, pattern, string.Empty);

                    // Clean up any trailing commas in arrays
                    jsonString = Regex.Replace(jsonString, @"\,\s*(\]|\})", "$1");
                    jsonString = Regex.Replace(jsonString, @"\[\s*,", "[");
                    return jsonString;
                }

                return jsonString.Replace(toReplace, value);
            // required string
            case "string":
                return jsonString.Replace(toReplace, model.TryGetStringPropertyValue(property) ?? string.Empty);
            case "int":
                return jsonString.Replace(toReplace, model.TryGetIntPropertyValue(property)?.ToString() ?? string.Empty);
            case "file":
                return jsonString.ReplaceForFile(toReplace, model, fileUrl);
            default:
                return jsonString;
        }
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