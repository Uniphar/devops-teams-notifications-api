using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Teams.Notifications.Api.Extensions;

internal static class StringExtensions
{
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? str) => string.IsNullOrWhiteSpace(str);

    public static string Join(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);

    public static bool ContainsAll(this string str, ReadOnlySpan<string> substrings, StringComparison comparison)
    {
        foreach (var substring in substrings)
            if (!str.Contains(substring, comparison))
                return false;

        return true;
    }

    public static bool TryGetUtf8String(this Span<byte> bytes, [NotNullWhen(true)] out string? str)
    {
        try
        {
            str = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            str = null;
            return false;
        }
    }
}