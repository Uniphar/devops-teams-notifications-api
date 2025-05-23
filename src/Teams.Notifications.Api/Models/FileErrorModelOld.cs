using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Teams.Notifications.Api.Models;

/// <summary>
///     File Error Message
/// </summary>
public sealed record FileErrorModelOld
{
    /// <summary>
    /// The file to upload
    /// </summary>
    [FromForm]
    [JsonPropertyName("file")]
    public IFormFile? File { get; set; }
    
    /// <summary>
    ///     The file name
    /// </summary>
    /// <example>Test.txt</example>
    [FromForm]
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    /// <summary>
    ///     System that went wrong
    /// </summary>
    /// <example>FrontGateExample</example>
    [FromForm]
    [JsonPropertyName("system")]
    public required string System { get; init; }

    /// <summary>
    ///     The job name associated
    /// </summary>
    /// <example>file-moving-example</example>
    [FromForm]
    [JsonPropertyName("jobId")]
    public required string JobId { get; init; }

    /// <summary>
    ///     The error message
    /// </summary>
    /// <example>StackTrace: Found and error in </example>
    [FromForm]
    [JsonPropertyName("errorMessage")]

    public string? ErrorMessage { get; set; } 

    /// <summary>
    ///     When the file originally went into error
    /// </summary>
    [FromForm]
    [JsonPropertyName("originalErrorTimestamp")]
    public DateTime? OriginalErrorTimestamp { get; set; }

    /// <summary>
    ///     The status
    /// </summary>
    [DefaultValue(FileErrorStatusEnum.Failed)]
    [FromForm]
    [JsonPropertyName("status")]
    public required FileErrorStatusEnum Status { get; set; }
}

public static class FileErrorModelExtensions
{
    public static string GetId(this FileErrorModelOld input)
    {
        // Combine with |
        var combined = $"{input.System}|{input.JobId}|{input.FileName}";

        // Compute SHA256 hash
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hashBytes = SHA256.HashData(bytes);

        // Convert hash to hexadecimal string
        var sb = new StringBuilder();
        foreach (var b in hashBytes) sb.Append(b.ToString("x2"));

        // we don't need a string too long
        return sb.ToString();
    }
}