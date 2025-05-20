using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Teams.Notifications.AdaptiveCardGen
{
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
    }
}
