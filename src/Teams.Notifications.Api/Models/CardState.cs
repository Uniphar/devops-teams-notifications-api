namespace Teams.Notifications.Api.Models;

public class CardState
{
    public required FileErrorModel FileError { get; set; }
    public required string TeamName { get; set; }
    public required string ChannelName { get; set; }
    public required string TeamId { get; set; }
    public required string ChannelId { get; set; }
    public required string MessageId { get; set; }
}