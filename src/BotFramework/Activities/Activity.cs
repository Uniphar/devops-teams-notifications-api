using System.Diagnostics;
using Teams.Cards.BotFramework.Serialization;

namespace Teams.Cards.BotFramework;

[JsonConverter(typeof(ActivityConverter))]
public abstract record Activity
{
	[JsonInclude, DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal abstract string Type { get; }

	[CacheDeserializedInstances]
	public Uri? ServiceUrl { get; init; }
	public DateTimeOffset? Timestamp { get; init; }
	public TeamsChannelData? ChannelData { get; init; }
}