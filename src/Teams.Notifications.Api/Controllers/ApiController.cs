using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Notifications.Api.Controllers;

// ASP.Net Controller that receives incoming HTTP requests from the Azure Bot Service or other configured event activity protocol sources.
// When called, the request has already been authorized and credentials and tokens validated.
[ApiController]
[Route("api/messages")]
public class ApiController(IAgentHttpAdapter adapter, IAgent bot) : ControllerBase
{
    [HttpPost] public Task PostAsync(CancellationToken cancellationToken) => adapter.ProcessAsync(Request, Response, bot, cancellationToken);
}