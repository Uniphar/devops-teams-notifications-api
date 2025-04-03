using Teams.Cards.BotFramework.Serialization;

namespace Teams.Cards.BotFramework;

public sealed record TeamsChannelData
{
	public ChannelInfo? Channel { get; init; }
	public string? EventType { get; init; }

	[ObjectTuple("id", "aadGroupId", "name")]
	public (string TeamsId, Guid AadGroupId, string Name)? Team { get; init; }

	public NotificationInfo? Notification { get; init; }

	[ObjectTuple("id")]
	public Guid? Tenant { get; init; }

	[ObjectTuple("SelectedChannel")]
	public ChannelInfo? Settings { get; init; }

	public ImmutableArray<OnBehalfOf>? OnBehalfOf { get; init; }
}

public sealed record ChannelInfo(string Id, string Name, ChannelType Type);

public enum ChannelType
{
	[JsonStringEnumMemberName("standard")] Standard,
	[JsonStringEnumMemberName("shared")] Shared,
	[JsonStringEnumMemberName("private")] Private
}

public sealed record NotificationInfo
{
	/// <summary>Whether the notification is to be sent to the user.</summary>
	public bool? Alert { get; init; }

	/// <summary>Whether the notification is to be shown to the user even if they are in a meeting.</summary>
	public bool? AlertInMeeting { get; init; }

	public Uri? ExternalResourceUrl { get; init; }
}

public sealed record OnBehalfOf
{
	[JsonPropertyName("itemid")]
	public int ItemId { get; init; }

	public string MentionType { get; init; } = "person";

	/// <summary>
	/// Message resource identifier (MRI) of the person on whose behalf the message is sent.
	/// Message sender name would appear as "[user] through [bot name]".
	/// </summary>
	[JsonPropertyName("mri")]
	public string? MessageResourceIdentifier { get; init; }

	/// <summary>The name of the person. Used as fallback in case name resolution is unavailable.</summary>
	public string? DisplayName { get; init; }
}

/// <summary>Role of the entity behind the account.</summary>
public enum ChannelAccountRole
{
	[JsonStringEnumMemberName("user")] User,
	[JsonStringEnumMemberName("bot")] Bot
}