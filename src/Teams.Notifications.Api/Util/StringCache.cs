namespace Teams.Cards.BotFramework.Utils;

internal sealed class StringCache
{
	private static ConcurrentDictionary<string, string> Cache { get; } = new ConcurrentDictionary<string, string>();

	public string GetString(ReadOnlySpan<char> chars)
	{
		if (Cache.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(chars, out var cachedString))
			return cachedString;

		var str = new string(chars);
		return Cache.GetOrAdd(str, str);
	}
}