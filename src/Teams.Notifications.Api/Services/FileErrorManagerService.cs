using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Services;

public sealed class FileErrorManagerService(IChannelAdapter adapter, ITeamsManagerService teamsManagerService, IConfiguration config) : IFileErrorManagerService
{
    private readonly string _clientId = config["AZURE_CLIENT_ID"] ?? throw new ArgumentNullException(config["AZURE_CLIENT_ID"]);
    private readonly string _tenantId = config["AZURE_TENANT_ID"] ?? throw new ArgumentNullException(config["AZURE_TENANT_ID"]);

    public async Task CreateUpdateOrDeleteFileErrorCardAsync(FileErrorModel fileError, string teamId, string channelId)
    {
        var url = fileError.File != null
            ? await teamsManagerService.UploadFile(teamId, channelId, "error/" + fileError.FileName, fileError.File.OpenReadStream())
            : await teamsManagerService.GetFileUrl(teamId, channelId, "error/" + fileError.FileName);
 

        var activity = new Activity
        {
            Type = "message",
            Attachments = new List<Attachment>
            {
                new()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = AdaptiveCardBuilder.CreateFileProcessingCard(fileError, url).ToJson()
                }
            }
        };
        var conversationReference = new ConversationReference
        {
            ChannelId = Channels.Msteams,
            ServiceUrl = $"https://smba.trafficmanager.net/emea/{_tenantId}",
            Conversation = new ConversationAccount(id: channelId),
            ActivityId = channelId
        };
        var id = await teamsManagerService.GetMessageId(teamId, channelId, fileError);
        // found an existing card so update id
        if (!string.IsNullOrWhiteSpace(id))
        {
            activity.Id = id;
            conversationReference.ActivityId = id;
        }


        await adapter.ContinueConversationAsync(_clientId,
            conversationReference,
            async (turnContext, cancellationToken) =>
            {
                // delete action
                if (fileError.Status == FileErrorStatusEnum.Success)
                {
                    // only delete if we actually found a message to delete
                    if (!string.IsNullOrWhiteSpace(activity.Id))
                        await turnContext.DeleteActivityAsync(activity.Id, cancellationToken);
                    return;
                }

                // no activity id so new one
                if (string.IsNullOrWhiteSpace(activity.Id))
                {
                    await turnContext.SendActivityAsync(activity, cancellationToken);
                    return;
                }

                // activity has an id so we need to update
                await turnContext.UpdateActivityAsync(activity, cancellationToken);
            },
            CancellationToken.None);
    }
}