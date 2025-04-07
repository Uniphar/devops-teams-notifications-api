namespace Teams.Notifications.Api.Card;

public sealed record Card
{
	public required Channel.Channel Channel { get; init; }
	public required string Id { get; init; }
	public required bool IsFinalised { get; init; }

}
