﻿namespace Teams.Notifications.Api.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]")]
[ApiController]
public class FileErrorController : ControllerBase
{
    private const string _teamName = "Notifications Platform";
    private const string _channelName = "File Errors";
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
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "You are doing something wrong!")]
    public async Task<IActionResult> Post(FileErrorModel fileError)
    {
        if (!IsFileExtensionValid(fileError))
            return BadRequest("Extension between uploaded file and filename needs to be equal");

        var teamId = await _managerService.GetTeamIdAsync(_teamName);
        var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
        await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);


        return Ok();
    }


    /// <summary>
    ///     Creates or updates the file error in teams
    /// </summary>
    /// <param name="fileError">The information about the file</param>
    [HttpPut]

    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "You are doing something wrong!")]
    public async Task<IActionResult> Put(FileErrorModel fileError)
    {
        if (!IsFileExtensionValid(fileError))
            return BadRequest("Extension between uploaded file and filename needs to be equal");


        var teamId = await _managerService.GetTeamIdAsync(_teamName);
        var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
        await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);


        return Ok();
    }

    /// <summary>
    ///     Deletes the file error in teams as long as you supply a success in the status
    /// </summary>
    /// <param name="fileError">The information about the file</param>
    [HttpDelete]
    [Produces("application/json")]
    // with swagger response you can give it a description
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "Creates a new file error or updates it")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "You are doing something wrong!")]
    public async Task<IActionResult> Delete(FileErrorModel fileError)
    {
        if (!IsFileExtensionValid(fileError))
            return BadRequest("Extension between uploaded file and filename needs to be equal");

        var teamId = await _managerService.GetTeamIdAsync(_teamName);
        var channelId = await _managerService.GetChannelIdAsync(teamId, _channelName);
        await _fileErrorService.CreateUpdateOrDeleteFileErrorCardAsync(fileError, teamId, channelId);

        return Ok();
    }

    private static bool IsFileExtensionValid(FileErrorModel fileError) => fileError.File == null || Path.GetExtension(fileError.File.FileName) == Path.GetExtension(fileError.FileName);
}