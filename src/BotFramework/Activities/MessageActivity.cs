namespace Teams.Cards.BotFramework;

public sealed record MessageActivity : Activity
{
	internal override string Type => "message";

	public required string Text { get; init; }
	public TextFormat Format { get; init; }

	public string? Locale { get; init; }

	public InputHint InputHint { get; init; }

	public ImmutableArray<Attachment> Attachments { get; init; } = ImmutableArray<Attachment>.Empty;

	public AttachmentLayout AttachmentLayout { get; init; }

	public string? Summary { get; init; }

	public Importance Important { get; init; }
}

public enum TextFormat
{
	[JsonStringEnumMemberName("plain")]	Plain,
	[JsonStringEnumMemberName("markdown")] Markdown,
	[JsonStringEnumMemberName("xml")] Xml
}

public enum InputHint
{
	[JsonStringEnumMemberName("accepting")] Accepting,
	[JsonStringEnumMemberName("expecting")] Expecting,
	[JsonStringEnumMemberName("ignoring")]  Ignoring
}

public enum AttachmentLayout
{
	[JsonStringEnumMemberName("list")] List,
	[JsonStringEnumMemberName("carousel")] Carousel
}

public enum Importance
{
	[JsonStringEnumMemberName("normal")] Normal,
	[JsonStringEnumMemberName("low")] Low,
	[JsonStringEnumMemberName("high")] High
}

public abstract record Attachment(string ContentType)
{
	[JsonInclude]
	public string ContentType { get; } = ContentType;
	public string? Name { get; init; }
	public Uri? ThumbnailUrl { get; init; }
}

public sealed record UrlAttachment([property: JsonInclude] Uri ContentUrl, string ContentType) : Attachment(ContentType);

public abstract record ContentAttachment<T>(string ContentType) : Attachment(ContentType)
{
	[JsonInclude]
	public required T Content { get; init; }
}

public sealed record AdaptiveCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.adaptive");
public sealed record AnimationCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.animation");
public sealed record AudioCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.audio");
public sealed record HeroCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.hero");
public sealed record ReceiptCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.receipt");
public sealed record SignInCardAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.signin");
public sealed record ThumbnailAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.thumbnail");
public sealed record VideoAttachment() : ContentAttachment<JsonDocument>("application/vnd.microsoft.card.video");