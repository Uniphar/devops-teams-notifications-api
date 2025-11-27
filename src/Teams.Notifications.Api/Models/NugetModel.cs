namespace Teams.Notifications.Api.Models
{
    class Root
    {
        [JsonPropertyName("data")]
        public PackageItem[]? Data { get; set; }
    }

    class PackageItem
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("id")]
        public string? PackageId { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("projectUrl")]
        public string? ProjectUrl { get; set; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }
        public static string NormalizeString(string value)
        {
            return value
                .Replace("\r\n", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\"", "\\\"");
        }

    }

}
