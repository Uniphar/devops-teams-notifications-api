namespace Teams.Notifications.Api.Controllers;

/// <summary>
///     DO NOT CHANGE THIS, this is used by the Teams Agent to communicate with the API
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/messages")]
[ApiController]
public class AgentController : ControllerBase
{
    private readonly ILogger<AgentController> _logger;

    public AgentController(ILogger<AgentController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "AgentScheme")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task PostMessages([FromServices] IAgentHttpAdapter adapter, [FromServices] IAgent agent, CancellationToken cancellationToken)
    {
        try
        {
            return adapter.ProcessAsync(Request, Response, agent, cancellationToken);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card action");
            throw;
        }
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "AgentScheme")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task GetMessages([FromServices] IAgentHttpAdapter adapter, [FromServices] IAgent agent, CancellationToken cancellationToken)
    {
        try
        {
            return adapter.ProcessAsync(Request, Response, agent, cancellationToken);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            throw;
        }
    }
}