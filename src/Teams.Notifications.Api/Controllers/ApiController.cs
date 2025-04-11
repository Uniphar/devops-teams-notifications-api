using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Notifications.Api.Controllers;

// ASP.Net Controller that receives incoming HTTP requests from the Azure Bot Service or other configured event activity protocol sources.
// When called, the request has already been authorized and credentials and tokens validated.
[ApiController]
[Route("api/messages")]
public class ApiController(IAgentHttpAdapter adapter, IAgent bot) : ControllerBase
{
    /// <summary>
    /// Handles HTTP POST and GET requests to process bot messages.
    /// </summary>
    /// <returns>A task that represents the work queued to execute.</returns>
    [HttpPost, HttpGet]
    public Task PostAsync()
    {
        // Delegate the processing of the HTTP POST to the adapter.
        // The adapter will invoke the bot.
        return adapter.ProcessAsync(Request, Response, bot);
    }
}