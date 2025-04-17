using System;
using System.Security.Cryptography;
using System.Text;

namespace Teams.Notifications.Api.Models;

public record FileErrorModel
{
    public required string FileName { get; init; }
    public required string System { get; init; }
    public required string JobId { get; init; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime? OriginalErrorTimestamp { get; set; }
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

public enum FileErrorStatusEnum
{
    Succes = 0, Failed = -1, InProgress = 1
}