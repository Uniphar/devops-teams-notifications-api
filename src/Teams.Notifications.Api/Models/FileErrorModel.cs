using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;

namespace Teams.Notifications.Api.Models;

/// <summary>
/// File Error Message
/// </summary>
public sealed record FileErrorModel
{
    /// <summary>
    ///     The file name
    /// </summary>
    /// <example>Test.txt</example>
    public required string FileName { get; init; }

    /// <summary>
    ///     System that went wrong
    /// </summary>
    /// <example>FrontGateExample</example>
    public required string System { get; init; }

    /// <summary>
    ///     The job name associated
    /// </summary>
    /// <example>file-moving-example</example>
    public required string JobId { get; init; }

    /// <summary>
    ///     The error message
    /// </summary>
    /// <example>StackTrace: Found and error in </example>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    ///     When the file originally went into error
    /// </summary>
    public DateTime? OriginalErrorTimestamp { get; set; }

    /// <summary>
    ///     The status
    /// </summary>
    [DefaultValue(FileErrorStatusEnum.Failed)]
    public required FileErrorStatusEnum Status { get; set; }
}

public static class FileErrorModelExtensions
{
    public static string GetId(this FileErrorModel input)
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

/// <summary>
/// Status of the error
/// </summary>
public enum FileErrorStatusEnum
{
    /// <summary>
    ///     Success
    /// </summary>
    Succes = 0,

    /// <summary>
    ///     Failed
    /// </summary>
    Failed = -1,

    /// <summary>
    ///     In progress
    /// </summary>
    InProgress = 1
}