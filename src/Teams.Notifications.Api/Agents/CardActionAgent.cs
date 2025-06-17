using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;


namespace Teams.Notifications.Api.Agents;

public class CardActionAgent : AgentApplication
{
    private readonly IFrontgateApiService _frontgateApiService;
    private readonly ITeamsManagerService _teamsManagerService;

    public CardActionAgent(
        AgentApplicationOptions options,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService
    ) : base(options)
    {
        _teamsManagerService = teamsManagerService;
        _frontgateApiService = frontgateApiService;
        AdaptiveCards.OnActionExecute(new Regex(".*?"), ProcessCardActionAsync);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)]
    protected async Task MemberAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Welcome new user"), cancellationToken);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Activity, Type = ActivityTypes.Message, Rank = RouteRank.Last)]
    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(turnContext.Activity.Text))
            await turnContext.SendActivityAsync(MessageFactory.Text("You are not meant to chat in this channel"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken
    )
    {
        var model = ProtocolJsonSerializer.ToObject<FileErrorprocessActionModel>(data);

        var teamId = turnContext.Activity.TeamsGetTeamInfo()?.Id;
        var channelId = turnContext.Activity.TeamsGetChannelId();

        if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(channelId))
            throw new InvalidOperationException("Team or Channel ID is missing from the context.");

        var nameAndStream = await _teamsManagerService.GetFileStreamAsync(teamId, channelId, model.PostFileStream);
        await using var stream = nameAndStream.Value;
        var fileName = nameAndStream.Key;
        // Upload the file to the external API
        var uploadResponse = await _frontgateApiService.UploadFileAsync(model.PostToUrl, stream, fileName);

        var message = uploadResponse.IsSuccessStatusCode
            ? model.PostSuccessMessage
            : $"Failed to upload file: {uploadResponse.ReasonPhrase}";

        var attachment = new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = message
        };
        var pendingActivity = new Activity
        {
            Type = "message",
            Id = turnContext.Activity.ReplyToId,
            Attachments = new List<Attachment> { attachment }
        };
        await turnContext.UpdateActivityAsync(pendingActivity, cancellationToken);
        return new AdaptiveCardInvokeResponse();
    }
}