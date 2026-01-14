namespace Teams.Notifications.Api.Services;

internal static class AdaptiveCardActionHandler
{
    internal static async Task<AdaptiveCardInvokeResponse> HandleLogAppProcessFile(this ITurnContext turnContext,
        object data,
        ICustomEventTelemetryClient telemetry,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService,
        ICardManagerService cardManagerService,
        CancellationToken cancellationToken
    )
    {
        using (telemetry.WithProperties([new("ActionExecute", "LogicAppErrorProcessActionModel")]))
        {
            telemetry.TrackEvent("ReprocessPressed");
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
                    telemetry.TrackEvent("LogAppProcessFile_NoTeamData");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                if (channel?.Name == null)
                {
                    telemetry.TrackEvent("LogAppProcessFile_NoChannelName");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                var teamId = teamDetails.AadGroupId;
                var channelName = channel.Name;

                if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(channelName)) throw new InvalidOperationException("Team or channelName is missing from the context.");

                var channelId = await teamsManagerService.GetChannelIdAsync(teamId, channelName, cancellationToken);
                var fileName = await teamsManagerService.GetFileNameAsync(teamId, channelId, model.PostFileLocation ?? string.Empty, cancellationToken);
                var groupUniqueName = await teamsManagerService.GetGroupNameUniqueName(teamId, cancellationToken);
                var teamName = await teamsManagerService.GetTeamName(teamId, cancellationToken);

                // in a conversation, the ReplyToId is the message id, instead of the normal id
                // see https://learn.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/cards-actions?tabs=csharp#example-of-incoming-invoke-message
                var messageId = turnContext.Activity.ReplyToId;

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
                    await cardManagerService.RemoveActionsFromCardAsync(teamId, channelId, messageId, ["Process"], cancellationToken);

                    telemetry.TrackEvent("ReprocessFileSuccess",
                        new()
                        {
                            ["Team"] = teamName,
                            ["Channel"] = channelName,
                            ["FileName"] = fileName,
                            ["MessageId"] = messageId
                        });

                    return AdaptiveCardInvokeResponseFactory.Message(model.PostSuccessMessage ?? "Success");
                }

                return AdaptiveCardInvokeResponseFactory.BadRequest($"Failed to send file: {await uploadResponse.Content.ReadAsStringAsync(cancellationToken)}");
            }
            catch (Exception ex)
            {
                telemetry.TrackEvent("LogAppProcessFile_Error",
                    new()
                    {
                        ["Error"] = ex.Message
                    });
                throw;
            }
        }
    }
}