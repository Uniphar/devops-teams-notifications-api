namespace Teams.Notifications.Api.Models;

public class CardState
{
    public FileErrorModel FileError { get; set; }
    public string TeamName { get; set; }
    public string ChannelName { get; set; }
    public string TeamId { get; set; }
    public string ChannelId { get; set; }
    public string MessageId { get; set; }
}