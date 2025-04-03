namespace Teams.Cards.BotFramework;

internal sealed class CachingFactory<TKey, TValue>(Func<TKey, TValue> factory)
	where TKey : IEquatable<TKey>
{
	private ConcurrentDictionary<TKey, TValue> Cache { get; } = new();

	public TValue Get(TKey key) => Cache.GetOrAdd(key, factory);
}