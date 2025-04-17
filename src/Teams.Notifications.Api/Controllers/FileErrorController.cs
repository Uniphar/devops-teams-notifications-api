using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileErrorController : ControllerBase
{    private readonly IFileErrorManagerService _fileErrorService;
    private readonly ILogger<FileErrorController> _logger;
    private readonly ITeamsManagerService _managerService;

    private const string _teamName = "Frontgate Files Moving Integration Test In";
    private const string _channelName = "General";


    public FileErrorController(IFileErrorManagerService fileErrorService,
        ITeamsManagerService managerService,
        ILogger<FileErrorController> logger
    )
    {
        _fileErrorService = fileErrorService;
        _managerService = managerService;
        _logger = logger;
    }


    /// <summary>
    ///     This controller will CREATE the initial
    /// </summary>
    /// <param name="fileError">Information that needs to be sent to teams</param>
    /// <returns>Hash code that can be used to update the error or delete it</returns>
    [HttpPost]
    public async Task<string> Post([FromBody] FileErrorModel fileError)
    {
       
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong creating the message");
            throw;
        }

        return fileError.GetId();
    }

    /// <summary>
    ///     This controller will update the FileErrorModel
    /// </summary>
    /// <param name="fileError">The file that you want to update</param>
    /// <returns></returns>
    [HttpPut]
    public async Task Put([FromBody] FileErrorModel fileError)
    {
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong updating the message");
            throw;
        }
    }

    /// <summary>
    ///     Deletes a given file error, you can also call the put with a "success"
    /// </summary>
    /// <param name="id">The hashcode from the creation step</param>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.DeleteFileErrorCard(id, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong deleting the message");
            throw;
        }
    }
}