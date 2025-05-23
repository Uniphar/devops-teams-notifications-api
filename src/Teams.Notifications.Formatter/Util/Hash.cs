using System.Security.Cryptography;

namespace Teams.Notifications.Formatter.Util;

internal static class Hash
{
    public static string Stream(Stream stream)
    {
        var position = stream.Position;
        var hash = Convert.ToHexString(SHA256.HashData(stream));
        stream.Position = position;
        return hash;
    }

    public static string File(string path)
    {
        using var file = System.IO.File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file));
    }
}