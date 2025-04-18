using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Teams.Notifications.Api.Models;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileErrorController : ControllerBase
{
    private const string _teamName = "Frontgate Files Moving Integration Test In";
    private const string _channelName = "General";
    private readonly IFileErrorManagerService _fileErrorService;
    private readonly ILogger<FileErrorController> _logger;
    private readonly ITeamsManagerService _managerService;


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
    ///     Creates or updates the file error in teams
    /// </summary>
    /// <param name="fileError">Information that needs to be sent to teams</param>
    [HttpPost]
    [Produces("application/json")]
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it", typeof(FileErrorModel))]
    public async Task<string> Post(FileErrorModel fileError)
    {
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong creating the message");
            throw;
        }

        return fileError.GetId();
    }

    /// <summary>
    ///     Creates or updates the file error in teams
    /// </summary>
    /// <param name="fileError">The information about the file</param>
    [HttpPut]
    [Produces("application/json")]
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it", typeof(FileErrorModel))]
    public async Task Put([FromBody] FileErrorModel fileError)
    {
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong updating the message");
            throw;
        }
    }

    /// <summary>
    ///     Deletes the file error in teams as long as you supply a success in the status
    /// </summary>
    /// <param name="fileError">The information about the file</param>
    [HttpDelete]
    [Produces("application/json")]
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it", typeof(FileErrorModel))]
    public async Task Delete([FromBody] FileErrorModel fileError)
    {
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(_teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
            await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong deleting the message");
            throw;
        }
    }
}