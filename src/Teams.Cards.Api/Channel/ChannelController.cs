namespace Teams.Cards.Api;

[Route("/team/{teamsId}/channel/{channelId}")]
public sealed class ChannelController(ChannelService ChannelService) : ControllerBase
{
	[HttpGet]
	public Task<ActionResult<Channel>> Get(string teamsId, string channelId)
	{

	}

	[HttpPost]
	public async Task<Channel> Create(string teamsId, string channelId, [FromBody] ChannelOptions channelOptions)
	{

	}

	[HttpPut]
	public async Task<Channel> Update(string teamsId, string channelId, [FromBody] ChannelOptions channelOptions)
	{

	}

	[HttpDelete]
	public async Task Delete(string teamsId, string channelId, [FromBody] ChannelOptions channelOptions)
	{

	}
}

public sealed record ChannelOptions
{
	public required string Name { get; init; }
	public string Description { get; init; }
}