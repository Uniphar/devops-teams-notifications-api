namespace Teams.Notifications.Api.Controllers;

/// <summary>
///     DO NOT CHANGE THIS, this is used by the Teams Agent to communicate with the API
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/messages")]
[ApiController]
public class AgentController(ILogger<AgentController> logger) : ControllerBase
{
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
            logger.LogError(ex, "Error processing card action");
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
            logger.LogError(ex, "Error processing message");
            throw;
        }
    }
}