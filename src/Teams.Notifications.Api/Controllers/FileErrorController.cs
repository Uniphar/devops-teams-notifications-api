using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Extensions.Teams.Models;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    private readonly ITeamsManagerService _managerService;
    private readonly string _clientId;
    private readonly string _tenantId;


    public FileErrorController(IFileErrorManagerService fileErrorService, ITeamsManagerService managerService, IConfiguration config, ILogger<FileErrorController> logger)
    {
        _fileErrorService = fileErrorService;
        _managerService = managerService;
        _logger = logger;

    }
    [HttpGet]
    public async Task<int> Get()
    {
        var fileError = new FileErrorModel
        {
            FileName = "Test-File.txt",
            System = "Test System",
            JobId = "Test Job",
            Status = FileErrorStatusEnum.Failed
        };
        var teamName = "Frontgate Files Moving Integration Test In";
        var channelName = "General";
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, channelName);
            await _fileErrorService.CreateFileErrorCardAsync(fileError, channelId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong creating the message");
            throw;
        }

        return fileError.GetHashCode();
    }
    /// <summary>
    ///     This controller will CREATE the initial
    /// </summary>
    /// <param name="fileError">Information that needs to be sent to teams</param>
    /// <returns>Hash code that can be used to update the error or delete it</returns>
    [HttpPost]
    public async Task<int> Post([FromBody] FileErrorModel fileError)
    {
        var teamName = "Frontgate Files Moving Integration Test In";
        var channelName = "General";
        try
        {
            var teamId = await _managerService.GetTeamIdAsync(teamName);
            var channelId = await _managerService.GetChannelIdAsync(teamId, channelName);
            await _fileErrorService.CreateFileErrorCardAsync(fileError, channelId);
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