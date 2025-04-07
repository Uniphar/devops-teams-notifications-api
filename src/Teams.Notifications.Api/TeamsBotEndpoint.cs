using Teams.Cards.BotFramework.Activities;
using Teams.Notifications.Api.Util;

namespace Teams.Notifications.Api;

//[Authorize("TeamsJwt")]
public sealed class TeamsBotEndpoint : ControllerBase
{
	[HttpPost("bot")]
	public async Task<ActionResult> IncomingBotActivity(Activity activity)
	{
		await Request.AppendToHttpFile(@"C:\\Code\\devops-teams-card-api\\src\\Teams.Cards.Api\\botCalls.http");
		return new StatusCodeResult(200);
	}
}