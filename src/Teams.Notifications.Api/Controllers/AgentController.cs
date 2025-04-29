namespace Teams.Notifications.Api.Controllers;

[Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
[Microsoft.AspNetCore.Mvc.ApiController]
[Authorize]
[Microsoft.AspNetCore.Mvc.Route("api/messages")]
public class AgentController(IAgentHttpAdapter adapter, IAgent bot) : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    ///     Handles HTTP POST and GET requests to process bot messages.
    /// </summary>
    /// <returns>A task that represents the work queued to execute.</returns>
    [Microsoft.AspNetCore.Mvc.HttpPost]
    [Microsoft.AspNetCore.Mvc.HttpGet]
    public Task PostAsync() =>
        // Delegate the processing of the HTTP POST to the adapter.
        // The adapter will invoke the bot.
        adapter.ProcessAsync(Request, Response, bot);
}