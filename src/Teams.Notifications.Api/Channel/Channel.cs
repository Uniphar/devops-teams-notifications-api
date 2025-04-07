namespace Teams.Notifications.Api.Channel;

public sealed record Channel
{
	public required string TeamId { get; init; }
	public required string ChannelId {	get; init; }
	public required Guid OwningApplication { get; init; }
}