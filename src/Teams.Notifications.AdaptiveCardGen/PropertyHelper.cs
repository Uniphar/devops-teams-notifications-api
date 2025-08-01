using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Teams.Notifications.AdaptiveCardGen;

public static class PropertyHelper
{
    private static readonly Regex MustacheRegex = new("{{(?<name>.*?):(?<type>.*?)}}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    ///     Gives back the props, eg { "Title": "Bla"} will return "Title"
    /// </summary>
    /// <param name="json">compliant json</param>
    /// <returns>List of the props</returns>
    public static List<PropWithMustache> ExtractPropertiesFromJson(this string json)
    {
        using var doc = JsonDocument.Parse(json);

        return (from property in doc.RootElement.EnumerateObject()
            let value = property.Value.GetString()
            where !string.IsNullOrEmpty(value)
            select new PropWithMustache { Property = property.Name, MustacheProperties = GetMustachePropertiesFromString(value).FirstOrDefault() }).ToList();
    }

    /// <summary>
    ///     Very simple regex to go from {{name:type}} to a list of properties with their types
    /// </summary>
    /// <param name="content"></param>
    /// <returns>Distinct list of all properties in the string</returns>
    public static Dictionary<string, string> GetMustachePropertiesFromString(this string content)
    {
        // 
        var matches = MustacheRegex.Matches(content);
        var properties = matches
            .Cast<Match>()
            .Select(x => new { name = x.Groups["name"].Value, type = x.Groups["type"].Value })
            .DistinctByProps(x => x.name);
        return properties.ToDictionary(m => m.name, m => m.type);
    }

    /// <summary>
    ///     checks if the types are valid, atm int, string or file
    /// </summary>
    /// <param name="nameAndType"> types you want to check</param>
    /// <param name="wrongItems">Items that are invalid</param>
    /// <returns>True if no mismatches were found</returns>
    public static bool IsValidTypes(this Dictionary<string, string> nameAndType, out Dictionary<string, string> wrongItems)
    {
        //name is key, type is value, due to dict
        wrongItems = nameAndType
            .Where(x => x.Value is not
                ("int" or "string" or "file")
            )
            .ToDictionary(x => x.Key, x => x.Value);

        return !wrongItems.Any();
    }

    /// <summary>
    ///     Files are uniquely named, this checks that
    /// </summary>
    /// <param name="nameAndType">Full list of props</param>
    /// <param name="wrongItems">Any wrong FILE prop </param>
    /// <returns>true if the files props are correct</returns>
    public static bool IsValidFile(this Dictionary<string, string> nameAndType, out Dictionary<string, string> wrongItems)
    {
        wrongItems = nameAndType.Where(x => x is { Value: "file", Key: not ("FileUrl" or "FileName") }).ToDictionary(x => x.Key, x => x.Value);
        return !wrongItems.Any();
    }

    /// <summary>
    ///     checks if the list has any file template, which is either FileUrl or FileName
    /// </summary>
    /// <param name="nameAndType"></param>
    /// <returns></returns>
    public static bool HasFileTemplate(this Dictionary<string, string> nameAndType)
    {
        return nameAndType.Any(x => x is { Value: "file", Key: "FileUrl" or "FileName" });
    }
}

public record PropWithMustache
{
    public string Property { get; set; }
    public KeyValuePair<string, string>? MustacheProperties { get; set; }
}