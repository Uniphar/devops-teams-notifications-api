using Microsoft.Agents.Client;
using Microsoft.Agents.Extensions.Teams.App;

namespace Teams.Notifications.Api.Agents;

public class CardActionAgent : AgentApplication
{
    private readonly IFrontgateApiService _frontgateApiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CardActionAgent> _logger;
    private readonly ITeamsManagerService _teamsManagerService;
    private readonly ICustomEventTelemetryClient _telemetry;

    public CardActionAgent(
        AgentApplicationOptions options,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService,
        ICustomEventTelemetryClient telemetry,
        ILogger<CardActionAgent> logger,
        IHttpClientFactory httpClientFactory
    ) : base(options)
    {
        _telemetry = telemetry;
        _logger = logger;
        _teamsManagerService = teamsManagerService;
        _frontgateApiService = frontgateApiService;
        _httpClientFactory = httpClientFactory;
        AdaptiveCards.OnActionExecute(new Regex(".*?"), ProcessCardActionAsync);
  
    }

    
    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.ReactionAdded)]
    protected async Task OnMessageReaction(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync("Message Reaction: " + turnContext.Activity.ReactionsAdded[0].Type, cancellationToken: cancellationToken);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)]
    protected async Task WelcomeMessageToUserAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
        var user = member.Name ?? "new user";
        await turnContext.SendActivityAsync(MessageFactory.Text("Welcome " + user), cancellationToken);
    }

   

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Activity, Type = ActivityTypes.Message, Rank = RouteRank.Last)]
    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(turnContext.Activity.Text)) 
            await turnContext.SendActivityAsync(MessageFactory.Text("We don't support any more interaction"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken
    )
    {
        using (_telemetry.WithProperties([new("ActionExecute", "LogicAppErrorProcessActionModel")]))
        {
            try
            {
                var model = ProtocolJsonSerializer.ToObject<LogicAppErrorProcessActionModel>(data);
                var teamsChannelData = turnContext.Activity.GetChannelData<TeamsChannelData>();

                var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, cancellationToken: cancellationToken);
                var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, cancellationToken: cancellationToken);
                var channel = channels.FirstOrDefault(x => x.Id == teamsChannelData.Channel.Id);

                // Guard against null channel data, which can occur when the json can't be deserialized
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (teamDetails is null)
                {
                    _logger.LogWarning("No team data for this file");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                if (channel?.Name == null)
                {
                    _logger.LogWarning("Could not load channel name");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                var teamId = teamDetails.AadGroupId;
                var channelName = channel.Name;

                if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(channelName)) throw new InvalidOperationException("Team or channelName is missing from the context.");

                var channelId = await _teamsManagerService.GetChannelIdAsync(teamId, channelName, cancellationToken);
                var fileName = await _teamsManagerService.GetFileNameAsync(teamId, channelId, model.PostFileLocation ?? string.Empty, cancellationToken);
                var groupUniqueName = await _teamsManagerService.GetGroupNameUniqueName(teamId, cancellationToken);
                var teamName = await _teamsManagerService.GetTeamName(teamId, cancellationToken);

                var fileInfo = new LogicAppFrontgateFileInformation
                {
                    file_name = fileName,
                    storage_reference = groupUniqueName,
                    initial_display_name = teamName,
                    storage_folder = $"/{channelName}/error/"
                };
                // Upload the file to the external API
                var uploadResponse = await _frontgateApiService.UploadFileAsync(model.PostOriginalBlobUri ?? string.Empty, fileInfo, cancellationToken);

                return uploadResponse.IsSuccessStatusCode
                    ? AdaptiveCardInvokeResponseFactory.Message(model.PostSuccessMessage ?? "Succes")
                    : AdaptiveCardInvokeResponseFactory.BadRequest($"Failed to sent file: {uploadResponse.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card action");
                throw;
            }
        }
    }
}