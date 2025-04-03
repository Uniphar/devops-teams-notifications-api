using Teams.Cards.BotFramework.Serialization;

namespace Teams.Cards.BotFramework;

[JsonConverter(typeof(IdConverter))]
public sealed record class Id<TKey> where TKey : notnull
{
	private readonly TKey id;

	public Id(TKey id)
	{
		this.id = id;
	}

	public static implicit operator TKey(Id<TKey> id) => id.id;
	public override string? ToString() => id.ToString();
}