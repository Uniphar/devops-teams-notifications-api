using System.Text.Json.Nodes;

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
                if (!string.IsNullOrEmpty(value)) return jsonString.Replace(toReplace, value);
                // Parse JSON and remove objects from arrays where the property value matches the placeholder
                var root = JsonNode.Parse(jsonString);
                root = RemoveObjectsWithPlaceholder(root, toReplace);
                return root?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? jsonString;

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

    // Recursively remove objects from arrays where the property value matches the placeholder
    private static JsonNode? RemoveObjectsWithPlaceholder(JsonNode? node, string toReplace)
    {
        switch (node)
        {
            case JsonArray array:
            {
                foreach (var arrayItem in array)
                    if (arrayItem is JsonObject obj && ObjectContainsPlaceholder(obj, toReplace))
                        array.Remove(arrayItem);
                    else
                        RemoveObjectsWithPlaceholder(arrayItem, toReplace);
                break;
            }
            case JsonObject obj:
            {
                foreach (var prop in obj)
                    RemoveObjectsWithPlaceholder(prop.Value, toReplace);
                break;
            }
        }

        return node;
    }

    // Helper: Recursively checks if any value in the object matches
    private static bool ObjectContainsPlaceholder(JsonObject obj, string toReplace)
    {
        foreach (var prop in obj)
            switch (prop.Value)
            {
                case JsonValue value when value.ToString().Contains(toReplace):
                case JsonObject childObj when ObjectContainsPlaceholder(childObj, toReplace):
                    return true;
                case JsonArray arr:
                {
                    foreach (var item in arr)
                        switch (item)
                        {
                            case JsonObject arrObj when ObjectContainsPlaceholder(arrObj, toReplace):
                            case JsonValue arrVal when arrVal.ToString().Contains(toReplace):
                                return true;
                        }

                    break;
                }
            }

        return false;
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