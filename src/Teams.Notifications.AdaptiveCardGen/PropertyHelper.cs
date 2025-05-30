using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Teams.Notifications.AdaptiveCardGen;

public static class PropertyHelper
{
    public static Dictionary<string, string> GetPropertiesFromJson(this string content)
    {
        // very simple regex where {{name:type}} means is that what you want, it has to be C# compatible, otherwise it will break
        var matches = Regex.Matches(content, "{{(?<name>.*?):(?<type>.*?)}}");
        var properties = matches
            .Cast<Match>()
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["type"].Value);
        return properties;
    }

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

    public static bool IsValidFile(this Dictionary<string, string> nameAndType, out Dictionary<string, string> wrongItems)
    {
        wrongItems = nameAndType.Where(x => x is { Value: "file", Key: not ("FileUrl" or "FileName") }).ToDictionary(x => x.Key, x => x.Value);
        return !wrongItems.Any();
    }

    public static bool HasFileTemplate(this Dictionary<string, string> nameAndType)
    {
        return nameAndType.Any(x => x is { Value: "file", Key: "FileUrl" or "FileName" });
    }
}