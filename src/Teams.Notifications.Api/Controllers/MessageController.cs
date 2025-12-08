namespace Teams.Notifications.Api.Controllers;

[ApiController]
[Authorize(Policy = "Teams.Notifications.Api.Writer", AuthenticationSchemes = "NotificationScheme")]
public class MessageController : ControllerBase
{
    private readonly ICardManagerService _cardManagerService;
    private readonly ICustomEventTelemetryClient _telemetry;

    public MessageController(ICardManagerService cardManagerService, ICustomEventTelemetryClient telemetry)
    {
        _cardManagerService = cardManagerService;
        _telemetry = telemetry;
    }

    /// <summary>
    ///     Creates or updates card as message in teams
    /// </summary>
    /// <param name="message">Message you want to send to the user</param>
    /// <param name="user">User you want to send a card to</param>
    /// <param name="cancellationToken">CancellationToken for when the application stops, mostly used for the bot</param>
    [HttpPost("MessageToUser")]
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = "Teams.Notifications.Api.Writer", AuthenticationSchemes = "NotificationScheme")]
    public async Task<IActionResult> PostUser(string message, [FromQuery(Name = "UserName")] string user, CancellationToken cancellationToken)
    {
        using (_telemetry.WithProperties([new("CreateOrUpdate", "MessageToUser")])) await _cardManagerService.CreateMessageToUserAsync(message, user, cancellationToken);
        return Ok();
    }
}