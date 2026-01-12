using AdaptiveCard = AdaptiveCards.AdaptiveCard;

namespace Teams.Notifications.Api.Services;

internal static class AdaptiveCardActionHandler
{
    internal static async Task<AdaptiveCardInvokeResponse> HandleLogAppProcessFile(this ITurnContext turnContext,
        object data,
        ICustomEventTelemetryClient telemetry,
        ILogger logger,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService,
        CancellationToken cancellationToken
    )
    {
        using (telemetry.WithProperties([new("ActionExecute", "LogicAppErrorProcessActionModel")]))
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
                    logger.LogWarning("No team data for this file");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                if (channel?.Name == null)
                {
                    logger.LogWarning("Could not load channel name");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                var teamId = teamDetails.AadGroupId;
                var channelName = channel.Name;

                if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(channelName)) throw new InvalidOperationException("Team or channelName is missing from the context.");

                var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, cancellationToken);
                var fileName = await teamsManagerService.GetFileNameAsync(teamId, channelId, model.PostFileLocation ?? string.Empty, cancellationToken);
                var groupUniqueName = await teamsManagerService.GetGroupNameUniqueName(teamId, cancellationToken);
                var teamName = await teamsManagerService.GetTeamName(teamId, cancellationToken);

                var fileInfo = new LogicAppFrontgateFileInformation
                {
                    file_name = fileName,
                    storage_reference = groupUniqueName,
                    initial_display_name = teamName,
                    storage_folder = $"/{channelName}/error/"
                };
                // Upload the file to the external API
                var uploadResponse = await frontgateApiService.UploadFileAsync(model.PostOriginalBlobUri ?? string.Empty, fileInfo, cancellationToken);

                if (uploadResponse.IsSuccessStatusCode)
                {
                    // Update the card to remove the process button
                    await RemoveProcessButton(turnContext, cancellationToken);
                    return AdaptiveCardInvokeResponseFactory.Message(model.PostSuccessMessage ?? "Success");
                }

                return AdaptiveCardInvokeResponseFactory.BadRequest($"Failed to send file: {uploadResponse.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing card action");
                throw;
            }
        }
    }

    private static async Task RemoveProcessButton(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        if (turnContext
                .Activity
                .Attachments
                .FirstOrDefault(x => x.ContentType == AdaptiveCard.ContentType)
                ?.Content is not JsonElement cardContent)
            return;

        var cardJson = cardContent.ToString();
        var card = AdaptiveCard.FromJson(cardJson).Card;

        // Remove the Process action
        var processAction = card.Actions.FirstOrDefault(a => a is AdaptiveExecuteAction { Verb: "Process" });
        if (processAction != null) card.Actions.Remove(processAction);

        var activity = new Activity
        {
            Type = "message",
            Id = turnContext.Activity.Id,
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card.ToJson()
                }
            }
        };

        await turnContext.UpdateActivityAsync(activity, cancellationToken);
    }
}