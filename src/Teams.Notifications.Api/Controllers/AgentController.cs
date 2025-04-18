using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Notifications.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Authorize]
[Route("api/messages")]
public class AgentController(IAgentHttpAdapter adapter, IAgent bot) : ControllerBase
{
    /// <summary>
    ///     Handles HTTP POST and GET requests to process bot messages.
    /// </summary>
    /// <returns>A task that represents the work queued to execute.</returns>
    [HttpPost]
    [HttpGet]
    public Task PostAsync() =>
        // Delegate the processing of the HTTP POST to the adapter.
        // The adapter will invoke the bot.
        adapter.ProcessAsync(Request, Response, bot);
}