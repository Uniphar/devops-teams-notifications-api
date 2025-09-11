﻿using Microsoft.Agents.Builder.App.AdaptiveCards;
using Microsoft.Agents.Extensions.Teams.Connector;
using Microsoft.Agents.Extensions.Teams.Models;
using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;


namespace Teams.Notifications.Api.Agents;

public class CardActionAgent : AgentApplication
{
    private readonly IFrontgateApiService _frontgateApiService;
    private readonly ILogger<CardActionAgent> _logger;
    private readonly ITeamsManagerService _teamsManagerService;
    private readonly ICustomEventTelemetryClient _telemetry;


    public CardActionAgent(
        AgentApplicationOptions options,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService,
        ICustomEventTelemetryClient telemetry,
        ILogger<CardActionAgent> logger
    ) : base(options)
    {
        _telemetry = telemetry;
        _logger = logger;
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
        if (!string.IsNullOrWhiteSpace(turnContext.Activity.Text))
            await turnContext.SendActivityAsync(MessageFactory.Text("You are not meant to chat in this channel"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken
    )
    {
        using (_telemetry.WithProperties(new { ActionExecute = "LogicAppErrorProcessActionModel" }))
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
                    _logger.LogWarning("No channel data found for this file");
                    return new AdaptiveCardInvokeResponse();
                }

                if (channel?.Name == null)
                {
                    _logger.LogWarning("Could not load channel name");
                    return new AdaptiveCardInvokeResponse();
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
                    ? AdaptiveCardInvokeResponseFactory.Message(model.PostSuccessMessage?? "Succes")
                    : AdaptiveCardInvokeResponseFactory.BadRequest($"Failed to sent file: {uploadResponse.ReasonPhrase}");

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
                //await turnContext.UpdateActivityAsync(pendingActivity, cancellationToken);

                
                return new AdaptiveCardInvokeResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card action");
                throw;
            }
    }
}