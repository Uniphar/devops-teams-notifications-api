using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Extensions.Teams.Models;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileErrorController : ControllerBase
{
    private readonly IFileErrorManagerService _fileErrorService;
    private readonly ILogger<FileErrorController> _logger;
    private readonly IChannelAdapter _adapter;

    public FileErrorController(IFileErrorManagerService fileErrorService, ILogger<FileErrorController> logger, IChannelAdapter adapter, ConcurrentDictionary<string, ConversationReference> conversationReferences )
    {
        _adapter = adapter;
        _fileErrorService = fileErrorService;
        _logger = logger;
    }

    public async Task<IActionResult> Get()
    {
        var tenantId = "8421dd92-337e-4405-8cfc-16118ffc5715";
        var clientId = "e50979f1-e66c-48fe-bdd9-ff0f634acc1";
        var teamChannelId = "19:19a98T0aX1b-w0aZgSDNOG6pkNkT0nkDmgHeKfvhBCk1@thread.tacv2";
        var refe = new ConversationReference
        {
            ChannelId = Channels.Msteams,
            ServiceUrl = $"https://smba.trafficmanager.net/emea/{tenantId}",
            
            Conversation = new ConversationAccount(id: teamChannelId),
            ActivityId = teamChannelId
        };
        await _adapter.ContinueConversationAsync(clientId, refe, BotCallback, CancellationToken.None);

        return new ContentResult
        {
            Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
            ContentType = "text/html",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync("proactive hello");
    }

    /// <summary>
    ///     This controller will CREATE the initial
    /// </summary>
    /// <param name="fileError">Information that needs to be sent to teams</param>
    /// <returns>Hash code that can be used to update the error or delete it</returns>
    [HttpPost]
    public async Task<int> Post([FromBody] FileErrorModel fileError)
    {
        try
        {
            await _fileErrorService.CreateFileErrorCard(fileError);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong creating the message");
            throw;
        }
        
        return fileError.GetHashCode();
    }

/// <summary>
/// 
/// </summary>
/// <param name="id">The existing hashcode from the error</param>
/// <param name="fileError">The file that you want to update</param>
/// <returns></returns>
    [HttpPut("{id}")]
public async Task Put(int id, [FromBody] FileErrorModel fileError)
{
    if (fileError.GetHashCode() != id) throw new ArgumentException("FileName, JobId and System cannot be changed after creating the error ");
    try
    {
        await _fileErrorService.UpdateFileErrorCard(id, fileError);
    }
    catch (Exception e)
    {
        _logger.LogError(e, "Something went wrong updating the message");
        throw;
    }
}

    /// <summary>
    ///     Deletes a given file error, the id is a hash that was generated during the create
    /// </summary>
    /// <param name="id">The hashcode from the creation step</param>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        try
        {
            await _fileErrorService.DeleteFileErrorCard(id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong deleting the message");
            throw;
        }
    }
}